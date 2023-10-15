using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using Fluxor.Selectors;
using Gdk;
using GLib;
using Glimpse.Components.Shared;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Gtk;
using Glimpse.Interop;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;
using Menu = Gtk.Menu;

namespace Glimpse.Components.StartMenu;

public class StartMenuLaunchIcon : EventBox
{
	private readonly FreeDesktopService _freeDesktopService;
	private readonly IDispatcher _dispatcher;
	private readonly Subject<EventButton> _buttonRelease = new();
	private readonly StartMenuWindow _startMenuWindow;
	private readonly Menu _contextMenu;

	public StartMenuLaunchIcon(FreeDesktopService freeDesktopService, IDispatcher dispatcher, IDisplayServer displayServer, IStore store)
	{
		var viewModelObservable = store.SubscribeSelector(StartMenuSelectors.ViewModel)
			.ToObservable()
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Replay(1);

		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;
		_contextMenu = new Menu() { ReserveToggleSize = false };
		_startMenuWindow = new StartMenuWindow(viewModelObservable, dispatcher);
		_startMenuWindow.KeepAbove = true;

		_startMenuWindow
			.SearchTextUpdated
			.TakeUntilDestroyed(this)
			.Subscribe(text => dispatcher.Dispatch(new UpdateStartMenuSearchTextAction() { SearchText = text }));

		_startMenuWindow
			.AppLaunch
			.TakeUntilDestroyed(this)
			.Subscribe(LaunchApp);

		_startMenuWindow
			.PowerButtonClicked
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.PowerButtonCommand).DistinctUntilChanged())
			.Subscribe(t =>
			{
				_startMenuWindow.ClosePopup();
				_freeDesktopService.Run(t.Second);
			});

		_startMenuWindow
			.SettingsButtonClicked
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.SettingsButtonCommand).DistinctUntilChanged())
			.Subscribe(t =>
			{
				_startMenuWindow.ClosePopup();
				_freeDesktopService.Run(t.Second);
			});

		_startMenuWindow
			.UserSettingsClicked
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.ActionBarViewModel.UserSettingsCommand).DistinctUntilChanged())
			.Subscribe(t =>
			{
				_startMenuWindow.ClosePopup();
				_freeDesktopService.Run(t.Second);
			});

		var startMenuWindowId = LibGdk3Interop.gdk_x11_window_get_xid(_startMenuWindow.Window.Handle);

		displayServer
			.FocusChanged
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Where(windowRef => _startMenuWindow.Visible && !_contextMenu.Visible && windowRef.Id != startMenuWindowId)
			.Subscribe(_ => _startMenuWindow.ClosePopup());

		_startMenuWindow
			.ContextMenuRequested
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable)
			.Subscribe(t => OpenContextMenu(t.First, t.Second));

		_buttonRelease
			.TakeUntilDestroyed(this)
			.Subscribe(_ => ToggleStartMenuWindow());

		displayServer
			.StartMenuOpened
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Subscribe(_ =>
			{
				Display.GetPointer(out var x, out var y);
				var eventMonitor = Window.Display.GetMonitorAtPoint(x, y);
				var windowMonitor = Window.Display.GetMonitorAtWindow(Window);

				if (eventMonitor == windowMonitor)
				{
					ToggleStartMenuWindow();
				}
			});

		_startMenuWindow.ObserveEvent(nameof(Hidden)).Subscribe(_ => StyleContext.RemoveClass("start-menu__launch-icon--open"));
		_startMenuWindow.ObserveEvent(nameof(Shown)).Subscribe(_ => StyleContext.AddClass("start-menu__launch-icon--open"));
		_startMenuWindow.ObserveEvent(nameof(VisibilityNotifyEvent)).Subscribe(_ => _startMenuWindow.CenterOnScreenAboveWidget(this));

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		this.AddClass("start-menu__launch-icon");

		var image = new Image();
		image.SetSizeRequest(42, 42);
		Add(image);

		var iconObservable = Observable.Return((Assets.MenuIcon.Scale(38), Assets.MenuIcon.Scale(32))).Replay(1);
		this.AppIcon(image, iconObservable);
		this.ObserveEvent<ButtonReleaseEventArgs>(nameof(ButtonReleaseEvent)).Subscribe(e =>
		{
			_buttonRelease.OnNext(e.Event);
			e.RetVal = true;
		});

		viewModelObservable.Connect();
		iconObservable.Connect();
	}

	private void ToggleStartMenuWindow()
	{
		if (_startMenuWindow.IsVisible)
		{
			_startMenuWindow.ClosePopup();
			this.RemoveClass("start-menu__launch-icon--open");
		}
		else
		{
			_startMenuWindow.Popup();
			StyleContext.AddClass("start-menu__launch-icon--open");
		}
	}

	private void LaunchApp(DesktopFile desktopFile)
	{
		_startMenuWindow.Hide();
		_freeDesktopService.Run(desktopFile);
	}

	private void OpenContextMenu(DesktopFile desktopFile, StartMenuViewModel startMenuViewModel)
	{
		var menuItems = ContextMenuHelper.CreateDesktopFileActions(desktopFile, IconTheme.GetForScreen(Screen));

		menuItems.ForEach(m =>
		{
			var action = (DesktopFileAction)m.Data["DesktopFileAction"];
			m.ObserveButtonRelease().Subscribe(_ => _freeDesktopService.Run(action));
		});

		var isPinnedToStart = startMenuViewModel.AllApps.Any(f => f.IsPinnedToStartMenu && f.DesktopFile == desktopFile);
		var isPinnedToTaskbar = startMenuViewModel.AllApps.Any(f => f.IsPinnedToTaskbar && f.DesktopFile == desktopFile);
		var pinStartIcon = isPinnedToStart ? Assets.UnpinIcon : Assets.PinIcon;
		var pinTaskbarIcon = isPinnedToTaskbar ? Assets.UnpinIcon : Assets.PinIcon;
		var pinStart = ContextMenuHelper.CreateMenuItem(isPinnedToStart ? "Unpin from Start" : "Pin to Start", Observable.Return(pinStartIcon.Scale(ThemeConstants.MenuItemIconSize)));
		pinStart.ObserveButtonRelease().Subscribe(_ => _dispatcher.Dispatch(new ToggleStartMenuPinningAction() { DesktopFile = desktopFile }));
		var pinTaskbar = ContextMenuHelper.CreateMenuItem(isPinnedToTaskbar ? "Unpin from taskbar" : "Pin to taskbar", Observable.Return(pinTaskbarIcon.Scale(ThemeConstants.MenuItemIconSize)));
		pinTaskbar.ObserveButtonRelease().Subscribe(_ => _dispatcher.Dispatch(new ToggleTaskbarPinningAction() { DesktopFile = desktopFile }));

		_contextMenu.RemoveAllChildren();

		if (menuItems.Any())
		{
			menuItems.ForEach(_contextMenu.Add);
			_contextMenu.Add(new SeparatorMenuItem());
		}

		_contextMenu.Add(pinStart);
		_contextMenu.Add(pinTaskbar);
		_contextMenu.ShowAll();
		_contextMenu.Popup();
	}
}
