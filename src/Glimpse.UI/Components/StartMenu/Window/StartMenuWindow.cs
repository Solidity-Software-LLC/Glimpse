using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using GLib;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Lib.System.Reactive;
using Glimpse.Redux;
using Glimpse.UI.State;
using Glimpse.Xorg.State;
using ReactiveMarbles.ObservableEvents;
using Key = Gdk.Key;
using WindowType = Gtk.WindowType;

namespace Glimpse.UI.Components.StartMenu.Window;

public class StartMenuWindow : Gtk.Window
{
	private readonly Subject<EventConfigure> _configureEventSubject = new();
	private readonly StartMenuContent _startMenuContent;

	public IObservable<Point> WindowMoved { get; }

	public StartMenuWindow(FreeDesktopService freeDesktopService, ReduxStore store)
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

		this.Events().DeleteEvent.Subscribe(e => e.RetVal = true);

		WindowMoved = _configureEventSubject
			.TakeUntilDestroyed(this)
			.Select(e => new Point(e.X, e.Y))
			.DistinctUntilChanged((a, b) => a.X == b.X && a.Y == b.Y);

		var viewModelObservable = store.Select(StartMenuSelectors.ViewModel)
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Replay(1);

		var actionBar = new StartMenuActionBar(viewModelObservable.Select(v => v.ActionBarViewModel).DistinctUntilChanged());

		this.ObserveEvent(actionBar.CommandInvoked).Subscribe(command =>
		{
			Hide();
			freeDesktopService.Run(command);
		});

		_startMenuContent = new StartMenuContent(viewModelObservable, actionBar);

		this.ObserveEvent(_startMenuContent.DesktopFileAction).Subscribe(a => freeDesktopService.Run(a));
		this.ObserveEvent(_startMenuContent.ChipActivated).Subscribe(c => store.Dispatch(new UpdateAppFilteringChip(c)));
		this.ObserveEvent(_startMenuContent.AppOrderingChanged).Subscribe(t => store.Dispatch(new UpdateStartMenuPinnedAppOrderingAction(t)));
		this.ObserveEvent(_startMenuContent.ToggleStartMenuPinning).Subscribe(f => store.Dispatch(new ToggleStartMenuPinningAction(f)));
		this.ObserveEvent(_startMenuContent.ToggleTaskbarPinning).Subscribe(f => store.Dispatch(new ToggleTaskbarPinningAction(f)));
		this.ObserveEvent(_startMenuContent.SearchTextUpdated).Subscribe(text => store.Dispatch(new UpdateStartMenuSearchTextAction(text)));
		this.ObserveEvent(_startMenuContent.AppLaunch).Subscribe(desktopFile =>
		{
			Hide();
			freeDesktopService.Run(desktopFile);
		});

		store.ObserveAction<WindowFocusedChangedAction>()
			.ObserveOn(new GLibSynchronizationContext())
			.TakeUntilDestroyed(this)
			.Where(action => IsVisible && action.WindowRef.Id != LibGdk3Interop.gdk_x11_window_get_xid(_startMenuContent.Window.Handle))
			.Subscribe(_ => Hide());

		store.ObserveAction<StartMenuOpenedAction>()
			.ObserveOn(new GLibSynchronizationContext())
			.TakeUntilDestroyed(this)
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
