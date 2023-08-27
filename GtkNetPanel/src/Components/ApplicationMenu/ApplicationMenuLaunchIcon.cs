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
	private readonly Subject<EventButton> _buttonRelease = new();

	public ApplicationMenuLaunchIcon(FreeDesktopService freeDesktopService, IDispatcher dispatcher, ApplicationMenuSelectors selectors)
	{
		var viewModelObservable = selectors
			.ViewModel
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));

		var contextMenu = new Menu();
		var appMenuWindow = new ApplicationMenuWindow(viewModelObservable);

		appMenuWindow
			.SearchTextUpdated
			.TakeUntilDestroyed(this)
			.Subscribe(text => dispatcher.Dispatch(new UpdateAppMenuSearchTextAction() { SearchText = text }));

		appMenuWindow
			.AppLaunch
			.TakeUntilDestroyed(this)
			.Subscribe(f =>
			{
				appMenuWindow.Hide();
				freeDesktopService.Run(f.Exec.FullExec);
			});

		Observable
			.FromEventPattern(appMenuWindow, nameof(appMenuWindow.FocusOutEvent))
			.TakeUntilDestroyed(this)
			.Where(c => !contextMenu.Visible)
			.Subscribe(_ => appMenuWindow.ClosePopup());

		appMenuWindow
			.ContextMenuRequested
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable)
			.Subscribe(t =>
			{
				PopulateContextMenu(contextMenu, t.First, t.Second);
				contextMenu.Popup();
			});

		_buttonRelease
			.TakeUntilDestroyed(this)
			.Where(_ => !appMenuWindow.Visible)
			.Subscribe(_ => appMenuWindow.Popup(), e => Console.WriteLine(e), () => { });

		Observable
			.FromEventPattern(appMenuWindow, nameof(appMenuWindow.VisibilityNotifyEvent))
			.TakeUntilDestroyed(this)
			.Subscribe(_ => appMenuWindow.CenterOnScreenAboveWidget(this));

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		StyleContext.AddClass("app-menu__launch-icon");
		SetSizeRequest(42, 42);
		this.AddHoverHighlighting();
		Add(new Image(Assets.Ubuntu.ScaleSimple(28, 28, InterpType.Bilinear)));
	}

	private void PopulateContextMenu(Menu contextMenu, DesktopFile desktopFile, ApplicationMenuViewModel applicationMenuViewModel)
	{
		var isPinnedToStart = applicationMenuViewModel.PinnedApps.Any(f => f == desktopFile);
		var isPinnedToTaskbar = applicationMenuViewModel.PinnedTaskbarApps.Any(f => f == desktopFile);
		contextMenu.RemoveAllChildren();
		contextMenu.Add(new MenuItem(isPinnedToStart ? "Unpin from Start" : "Pin to Start"));
		contextMenu.Add(new MenuItem(isPinnedToTaskbar ? "Unpin from taskbar" : "Pin to taskbar"));
		contextMenu.ShowAll();
	}

	protected override bool OnButtonReleaseEvent(EventButton evnt)
	{
		_buttonRelease.OnNext(evnt);
		return true;
	}
}
