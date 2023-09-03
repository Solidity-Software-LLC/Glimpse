using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using Gdk;
using GLib;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;
using Menu = Gtk.Menu;

namespace Glimpse.Components.StartMenu;

public class StartMenuLaunchIcon : Button
{
	private readonly FreeDesktopService _freeDesktopService;
	private readonly IDispatcher _dispatcher;
	private readonly Subject<EventButton> _buttonRelease = new();
	private readonly StartMenuWindow _startMenuWindow;
	private readonly Menu _contextMenu;

	public StartMenuLaunchIcon(FreeDesktopService freeDesktopService, IDispatcher dispatcher, StartMenuSelectors selectors, IDisplayServer displayServer)
	{
		var viewModelObservable = selectors
			.ViewModel
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));

		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;
		_contextMenu = new Menu() { ReserveToggleSize = false };
		_startMenuWindow = new StartMenuWindow(viewModelObservable);

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

		_startMenuWindow.ObserveEvent(nameof(FocusOutEvent))
			.Where(c => !_contextMenu.Visible)
			.Subscribe(_ => _startMenuWindow.ClosePopup());

		_startMenuWindow
			.ContextMenuRequested
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable)
			.Subscribe(t => OpenContextMenu(t.First, t.Second));

		_buttonRelease
			.TakeUntilDestroyed(this)
			.Where(_ => !_startMenuWindow.Visible)
			.Subscribe(_ =>
			{
				_startMenuWindow.Popup();
				StyleContext.AddClass("start-menu__launch-icon--open");
			});

		displayServer
			.StartMenuOpen
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Subscribe(coords =>
			{
				var eventMonitor = Window.Display.GetMonitorAtPoint(coords.x, coords.y);
				var windowMonitor = Window.Display.GetMonitorAtWindow(Window);

				if (eventMonitor == windowMonitor)
				{
					_startMenuWindow.Popup();
					StyleContext.AddClass("start-menu__launch-icon--open");
				}
			});

		_startMenuWindow.ObserveEvent(nameof(Hidden)).Subscribe(_ => StyleContext.RemoveClass("start-menu__launch-icon--open"));
		_startMenuWindow.ObserveEvent(nameof(Shown)).Subscribe(_ => StyleContext.AddClass("start-menu__launch-icon--open"));
		_startMenuWindow.ObserveEvent(nameof(VisibilityNotifyEvent)).Subscribe(_ => _startMenuWindow.CenterOnScreenAboveWidget(this));

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		StyleContext.AddClass("start-menu__launch-icon");
		Add(new Image(Assets.MenuIcon.ScaleSimple(38, 38, InterpType.Bilinear)));
	}

	private void LaunchApp(DesktopFile desktopFile)
	{
		_startMenuWindow.Hide();
		_freeDesktopService.Run(desktopFile);
	}

	private void OpenContextMenu(DesktopFile desktopFile, StartMenuViewModel startMenuViewModel)
	{
		var menuItems = ContextMenuHelper.CreateDesktopFileActions(desktopFile);

		menuItems.ForEach(m =>
		{
			var action = (DesktopFileAction)m.Data["DesktopFileAction"];
			m.ObserveButtonRelease().Subscribe(_ => _freeDesktopService.Run(action));
		});

		var isPinnedToStart = startMenuViewModel.PinnedStartApps.Any(f => f == desktopFile);
		var isPinnedToTaskbar = startMenuViewModel.PinnedTaskbarApps.Any(f => f == desktopFile);
		var pinStartIcon = isPinnedToStart ? Assets.UnpinIcon : Assets.PinIcon;
		var pinTaskbarIcon = isPinnedToTaskbar ? Assets.UnpinIcon : Assets.PinIcon;
		var pinStart = ContextMenuHelper.CreateMenuItem(isPinnedToStart ? "Unpin from Start" : "Pin to Start", pinStartIcon.ScaleSimple(16, 16, InterpType.Bilinear));
		pinStart.ObserveButtonRelease().Subscribe(_ => _dispatcher.Dispatch(new ToggleStartMenuPinningAction() { DesktopFile = desktopFile }));
		var pinTaskbar = ContextMenuHelper.CreateMenuItem(isPinnedToTaskbar ? "Unpin from taskbar" : "Pin to taskbar", pinTaskbarIcon.ScaleSimple(16, 16, InterpType.Bilinear));
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

	protected override bool OnButtonReleaseEvent(EventButton evnt)
	{
		_buttonRelease.OnNext(evnt);
		return true;
	}
}
