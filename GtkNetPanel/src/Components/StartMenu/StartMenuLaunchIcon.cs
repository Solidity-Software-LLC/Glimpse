using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.Extensions.Gtk;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;
using Menu = Gtk.Menu;
using Process = System.Diagnostics.Process;

namespace GtkNetPanel.Components.StartMenu;

public class StartMenuLaunchIcon : EventBox
{
	private readonly FreeDesktopService _freeDesktopService;
	private readonly IDispatcher _dispatcher;
	private readonly Subject<EventButton> _buttonRelease = new();
	private readonly StartMenuWindow _startMenuWindow;
	private readonly Menu _contextMenu;

	public StartMenuLaunchIcon(FreeDesktopService freeDesktopService, IDispatcher dispatcher, StartMenuSelectors selectors)
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
			.WithLatestFrom(viewModelObservable.Select(vm => vm.PowerButtonCommand).DistinctUntilChanged())
			.Subscribe(t => Process.Start(t.Second));

		Observable
			.FromEventPattern(_startMenuWindow, nameof(_startMenuWindow.FocusOutEvent))
			.TakeUntilDestroyed(this)
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
			.Subscribe(_ => _startMenuWindow.Popup());

		Observable
			.FromEventPattern(_startMenuWindow, nameof(_startMenuWindow.VisibilityNotifyEvent))
			.TakeUntilDestroyed(this)
			.Subscribe(_ => _startMenuWindow.CenterOnScreenAboveWidget(this));

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		StyleContext.AddClass("start-menu__launch-icon");
		SetSizeRequest(42, 42);
		this.AddHoverHighlighting();
		Add(new Image(Assets.MenuIcon.ScaleSimple(38, 38, InterpType.Bilinear)));
	}

	private void LaunchApp(DesktopFile desktopFile)
	{
		_startMenuWindow.Hide();
		_freeDesktopService.Run(desktopFile.Exec.FullExec);
	}

	private void OpenContextMenu(DesktopFile desktopFile, StartMenuViewModel startMenuViewModel)
	{
		var menuItems = ContextMenuHelper.CreateDesktopFileActions(desktopFile);

		menuItems.ForEach(m =>
		{
			var action = (DesktopFileAction)m.Data["DesktopFileAction"];

			Observable.FromEventPattern(m, nameof(m.ButtonReleaseEvent))
				.TakeUntilDestroyed(m)
				.Subscribe(_ => _freeDesktopService.Run(action));
		});

		var isPinnedToStart = startMenuViewModel.PinnedStartApps.Any(f => f == desktopFile);
		var isPinnedToTaskbar = startMenuViewModel.PinnedTaskbarApps.Any(f => f == desktopFile);
		var pinStartIcon = isPinnedToStart ? Assets.UnpinIcon : Assets.PinIcon;
		var pinTaskbarIcon = isPinnedToTaskbar ? Assets.UnpinIcon : Assets.PinIcon;
		var pinStart = ContextMenuHelper.CreateMenuItem(isPinnedToStart ? "Unpin from Start" : "Pin to Start", pinStartIcon.ScaleSimple(16, 16, InterpType.Bilinear));
		var pinTaskbar = ContextMenuHelper.CreateMenuItem(isPinnedToTaskbar ? "Unpin from taskbar" : "Pin to taskbar", pinTaskbarIcon.ScaleSimple(16, 16, InterpType.Bilinear));

		Observable.FromEventPattern(pinStart, nameof(pinStart.ButtonPressEvent))
			.TakeUntilDestroyed(pinStart)
			.Subscribe(_ => _dispatcher.Dispatch(new ToggleStartMenuPinningAction() { DesktopFile = desktopFile }));

		Observable.FromEventPattern(pinTaskbar, nameof(pinTaskbar.ButtonPressEvent))
			.TakeUntilDestroyed(pinTaskbar)
			.Subscribe(_ => _dispatcher.Dispatch(new ToggleTaskbarPinningAction() { DesktopFile = desktopFile }));

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
