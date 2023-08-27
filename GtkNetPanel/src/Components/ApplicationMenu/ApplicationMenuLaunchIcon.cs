using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;
using Menu = Gtk.Menu;
using MenuItem = Gtk.MenuItem;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuLaunchIcon : EventBox
{
	private readonly FreeDesktopService _freeDesktopService;
	private readonly Subject<EventButton> _buttonRelease = new();
	private readonly ApplicationMenuWindow _appMenuWindow;
	private readonly Menu _contextMenu;

	public ApplicationMenuLaunchIcon(FreeDesktopService freeDesktopService, IDispatcher dispatcher, ApplicationMenuSelectors selectors)
	{
		var viewModelObservable = selectors
			.ViewModel
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));

		_freeDesktopService = freeDesktopService;
		_contextMenu = new Menu();
		_appMenuWindow = new ApplicationMenuWindow(viewModelObservable);

		_appMenuWindow
			.SearchTextUpdated
			.TakeUntilDestroyed(this)
			.Subscribe(text => dispatcher.Dispatch(new UpdateAppMenuSearchTextAction() { SearchText = text }));

		_appMenuWindow
			.AppLaunch
			.TakeUntilDestroyed(this)
			.Subscribe(LaunchApp);

		Observable
			.FromEventPattern(_appMenuWindow, nameof(_appMenuWindow.FocusOutEvent))
			.TakeUntilDestroyed(this)
			.Where(c => !_contextMenu.Visible)
			.Subscribe(_ => _appMenuWindow.ClosePopup());

		_appMenuWindow
			.ContextMenuRequested
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable)
			.Subscribe(t => OpenContextMenu(t.First, t.Second));

		_buttonRelease
			.TakeUntilDestroyed(this)
			.Where(_ => !_appMenuWindow.Visible)
			.Subscribe(_ => _appMenuWindow.Popup());

		Observable
			.FromEventPattern(_appMenuWindow, nameof(_appMenuWindow.VisibilityNotifyEvent))
			.TakeUntilDestroyed(this)
			.Subscribe(_ => _appMenuWindow.CenterOnScreenAboveWidget(this));

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		StyleContext.AddClass("app-menu__launch-icon");
		SetSizeRequest(42, 42);
		this.AddHoverHighlighting();
		Add(new Image(Assets.Ubuntu.ScaleSimple(28, 28, InterpType.Bilinear)));
	}

	private void LaunchApp(DesktopFile desktopFile)
	{
		_appMenuWindow.Hide();
		_freeDesktopService.Run(desktopFile.Exec.FullExec);
	}

	private void OpenContextMenu(DesktopFile desktopFile, ApplicationMenuViewModel applicationMenuViewModel)
	{
		var isPinnedToStart = applicationMenuViewModel.PinnedApps.Any(f => f == desktopFile);
		var isPinnedToTaskbar = applicationMenuViewModel.PinnedTaskbarApps.Any(f => f == desktopFile);
		_contextMenu.RemoveAllChildren();
		_contextMenu.Add(new MenuItem(isPinnedToStart ? "Unpin from Start" : "Pin to Start"));
		_contextMenu.Add(new MenuItem(isPinnedToTaskbar ? "Unpin from taskbar" : "Pin to taskbar"));
		_contextMenu.ShowAll();
		_contextMenu.Popup();
	}

	protected override bool OnButtonReleaseEvent(EventButton evnt)
	{
		_buttonRelease.OnNext(evnt);
		return true;
	}
}
