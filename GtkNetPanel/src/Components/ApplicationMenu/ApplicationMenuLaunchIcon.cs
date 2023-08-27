using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;
using Menu = Gtk.Menu;
using MenuItem = Gtk.MenuItem;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuLaunchIcon : EventBox
{
	private readonly Subject<EventButton> _buttonRelease = new();

	public ApplicationMenuLaunchIcon(IState<RootState> stateObservable, FreeDesktopService freeDesktopService, IDispatcher dispatcher)
	{
		var allAppsObservable = stateObservable
			.ToObservable()
			.TakeUntilDestroyed(this)
			.Select(s => s.DesktopFiles)
			.DistinctUntilChanged()
			.Select(s => s.OrderBy(f => f.Name).Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec)).ToImmutableList());

		var pinnedAppsObservable = stateObservable
			.ToObservable()
			.TakeUntilDestroyed(this)
			.Select(s => s.ApplicationMenuState.PinnedDesktopFiles)
			.DistinctUntilChanged()
			.CombineLatest(allAppsObservable)
			.Select(t =>
			{
				(ImmutableList<DesktopFile> pinnedFiles, ImmutableList<DesktopFile> allApps) = t;
				return pinnedFiles.Select(pinnedFile => allApps.First(app => app == pinnedFile)).ToImmutableList();
			})
			.DistinctUntilChanged();

		var searchTextObservable = stateObservable
			.ToObservable()
			.TakeUntilDestroyed(this)
			.Select(s => s.ApplicationMenuState.SearchText)
			.DistinctUntilChanged();

		var appsToDisplayObservable = searchTextObservable
			.CombineLatest(pinnedAppsObservable, allAppsObservable)
			.Select(t => string.IsNullOrEmpty(t.First) ? t.Second : t.Third.Where(d => d.Name.Contains(t.First, StringComparison.InvariantCultureIgnoreCase)).ToImmutableList())
			.Do(_ => { }, e => Console.WriteLine(e));

		var viewModelObservable = pinnedAppsObservable
			.CombineLatest(allAppsObservable, searchTextObservable, appsToDisplayObservable)
			.Select(t => new ApplicationMenuViewModel() { PinnedFiles = t.First, DesktopFiles = t.Second, SearchText = t.Third, AppsToDisplay = t.Fourth })
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));

		var appMenuWindow = new ApplicationMenuWindow(viewModelObservable);

		appMenuWindow
			.SearchTextUpdated
			.TakeUntilDestroyed(this)
			.Subscribe(text =>
			{
				dispatcher.Dispatch(new UpdateAppMenuSearchTextAction() { SearchText = text });
			});

		appMenuWindow
			.AppLaunch
			.TakeUntilDestroyed(this)
			.Subscribe(f =>
			{
				appMenuWindow.Hide();
				freeDesktopService.Run(f.Exec.FullExec);
			});

		var contextMenu = new Menu();
		contextMenu.Add(new MenuItem("Pin to Start"));
		contextMenu.Add(new MenuItem("Pin to taskbar"));
		contextMenu.ShowAll();

		Observable
			.FromEventPattern(appMenuWindow, nameof(appMenuWindow.FocusOutEvent))
			.TakeUntilDestroyed(this).Subscribe(_ =>
			{
				if (!contextMenu.Visible)
				{
					appMenuWindow.ClosePopup();
				}
			});

		appMenuWindow
			.ContextMenuRequested
			.Subscribe(f =>
			{
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

	protected override bool OnButtonReleaseEvent(EventButton evnt)
	{
		_buttonRelease.OnNext(evnt);
		return true;
	}
}
