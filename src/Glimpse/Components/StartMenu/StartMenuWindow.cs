using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using Fluxor.Selectors;
using Gdk;
using GLib;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Interop;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Key = Gdk.Key;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.Components.StartMenu;

public class StartMenuWindow : Window
{
	private readonly Subject<EventConfigure> _configureEventSubject = new();
	private readonly StartMenuContent _startMenuContent;

	public IObservable<Point> WindowMoved { get; }

	public StartMenuWindow(FreeDesktopService freeDesktopService, IDisplayServer displayServer, IStore store, IDispatcher dispatcher)
		: base(WindowType.Toplevel)
	{
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = false;
		TypeHint = WindowTypeHint.Dialog;
		Visual = Screen.RgbaVisual;
		AppPaintable = true;
		Visible = false;
		KeepAbove = true;

		WindowMoved = _configureEventSubject
			.TakeUntilDestroyed(this)
			.Select(e => new Point(e.X, e.Y))
			.DistinctUntilChanged((a, b) => a.X == b.X && a.Y == b.Y);

		var viewModelObservable = store.SubscribeSelector(StartMenuSelectors.ViewModel)
			.ToObservable()
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Replay(1);

		_startMenuContent = new StartMenuContent(viewModelObservable);

		_startMenuContent.ChipActivated
			.TakeUntilDestroyed(this)
			.Subscribe(c => dispatcher.Dispatch(new UpdateAppFilteringChip(c)));

		_startMenuContent.AppOrderingChanged
			.TakeUntilDestroyed(this)
			.Subscribe(t => dispatcher.Dispatch(new UpdateStartMenuPinnedAppOrderingAction(t.Item1.DesktopFile.IniFile.FilePath, t.Item2)));

		_startMenuContent.ToggleStartMenuPinning
			.TakeUntilDestroyed(this)
			.Subscribe(f => dispatcher.Dispatch(new ToggleStartMenuPinningAction(f)));

		_startMenuContent.ToggleTaskbarPinning
			.TakeUntilDestroyed(this)
			.Subscribe(f => dispatcher.Dispatch(new ToggleTaskbarPinningAction(f)));

		_startMenuContent.SearchTextUpdated
			.TakeUntilDestroyed(this)
			.Subscribe(text => dispatcher.Dispatch(new UpdateStartMenuSearchTextAction(text)));

		_startMenuContent.AppLaunch
			.TakeUntilDestroyed(this)
			.Subscribe(desktopFile =>
			{
				Hide();
				freeDesktopService.Run(desktopFile);
			});

		_startMenuContent.PowerButtonClicked
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.PowerButtonCommand).DistinctUntilChanged())
			.Subscribe(t =>
			{
				Hide();
				freeDesktopService.Run(t.Second);
			});

		_startMenuContent.SettingsButtonClicked
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.SettingsButtonCommand).DistinctUntilChanged())
			.Subscribe(t =>
			{
				Hide();
				freeDesktopService.Run(t.Second);
			});

		_startMenuContent.UserSettingsClicked
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.UserSettingsCommand).DistinctUntilChanged())
			.Subscribe(t =>
			{
				Hide();
				freeDesktopService.Run(t.Second);
			});

		_startMenuContent.DesktopFileAction
			.TakeUntilDestroyed(this)
			.Subscribe(a => freeDesktopService.Run(a));

		displayServer.FocusChanged
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Where(windowRef => IsVisible && windowRef.Id != LibGdk3Interop.gdk_x11_window_get_xid(_startMenuContent.Window.Handle))
			.Subscribe(_ => Hide());

		displayServer.StartMenuOpened
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Subscribe(_ => ToggleVisibility());

		Add(_startMenuContent);
		viewModelObservable.Connect();
	}

	public void ToggleVisibility()
	{
		Display.GetPointer(out var x, out var y);

		var eventMonitor = Display.GetMonitorAtPoint(x, y);
		var eventMonitorDimension = eventMonitor.Geometry;
		var eventPanel = Application.Windows.OfType<Panel>().First(p =>
		{
			p.Window.GetRootCoords(0, 0, out var panelX, out _);
			return panelX >= eventMonitorDimension.Left && panelX <= eventMonitorDimension.Right;
		});

		if (IsVisible)
		{
			Window.GetRootCoords(0, 0, out var currentX, out _);

			if (currentX >= eventMonitorDimension.Left && currentX <= eventMonitorDimension.Right)
			{
				Hide();
			}
			else
			{
				this.CenterOnScreenAboveWidget(eventPanel);
			}
		}
		else
		{
			Show();
			this.CenterOnScreenAboveWidget(eventPanel);
		}
	}

	protected override bool OnConfigureEvent(EventConfigure evnt)
	{
		_configureEventSubject.OnNext(evnt);
		return base.OnConfigureEvent(evnt);
	}

	protected override void OnShown()
	{
		base.OnShown();
		_startMenuContent.HandleWindowShown();
	}

	[ConnectBefore]
	protected override bool OnKeyPressEvent(EventKey evnt)
	{
		if (evnt.Key == Key.Escape)
		{
			Visible = false;
			return true;
		}

		if (_startMenuContent.HandleKeyPress(evnt.KeyValue))
		{
			return true;
		}

		return base.OnKeyPressEvent(evnt);
	}
}
