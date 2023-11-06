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

		this.ObserveEvent(_startMenuContent.ChipActivated).Subscribe(c => dispatcher.Dispatch(new UpdateAppFilteringChip(c)));
		this.ObserveEvent(_startMenuContent.AppOrderingChanged).Subscribe(t => dispatcher.Dispatch(new UpdateStartMenuPinnedAppOrderingAction(t.Item1.DesktopFile.IniFile.FilePath, t.Item2)));
		this.ObserveEvent(_startMenuContent.ToggleStartMenuPinning).Subscribe(f => dispatcher.Dispatch(new ToggleStartMenuPinningAction(f)));
		this.ObserveEvent(_startMenuContent.ToggleTaskbarPinning).Subscribe(f => dispatcher.Dispatch(new ToggleTaskbarPinningAction(f)));
		this.ObserveEvent(_startMenuContent.SearchTextUpdated).Subscribe(text => dispatcher.Dispatch(new UpdateStartMenuSearchTextAction(text)));
		this.ObserveEvent(_startMenuContent.AppLaunch).Subscribe(desktopFile =>
		{
			Hide();
			freeDesktopService.Run(desktopFile);
		});

		Observable.Merge(
				_startMenuContent.PowerButtonClicked.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.PowerButtonCommand)),
				_startMenuContent.SettingsButtonClicked.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.SettingsButtonCommand)),
				_startMenuContent.UserSettingsClicked.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.UserSettingsCommand)))
			.TakeUntilDestroyed(this)
			.Subscribe(t =>
			{
				Hide();
				freeDesktopService.Run(t.Second);
			});

		this.ObserveEvent(_startMenuContent.DesktopFileAction).Subscribe(a => freeDesktopService.Run(a));
		this.ObserveEvent(displayServer.FocusChanged)
			.ObserveOn(new GLibSynchronizationContext())
			.Where(windowRef => IsVisible && windowRef.Id != LibGdk3Interop.gdk_x11_window_get_xid(_startMenuContent.Window.Handle))
			.Subscribe(_ => Hide());
		this.ObserveEvent(displayServer.StartMenuOpened)
			.ObserveOn(new GLibSynchronizationContext())
			.Subscribe(_ => ToggleVisibility());

		Add(_startMenuContent);
		viewModelObservable.Connect();
	}

	public void ToggleVisibility()
	{
		Display.GetPointer(out var x, out var y);
		var eventMonitor = Display.GetMonitorAtPoint(x, y);

		if (IsVisible && eventMonitor.Contains(Window))
		{
			Hide();
		}
		else
		{
			Show();
			var eventPanel = Application.Windows.OfType<Panel>().First(p => eventMonitor.Contains(p.Window));
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
