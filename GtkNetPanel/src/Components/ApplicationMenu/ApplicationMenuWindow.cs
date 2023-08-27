using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services;
using GtkNetPanel.Services.FreeDesktop;
using Key = Gdk.Key;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuWindow : Window
{
	private readonly Entry _hiddenEntry;
	private readonly Subject<DesktopFile> _appLaunch = new();
	private readonly Subject<DesktopFile> _contextMenuRequested = new();
	private readonly Subject<string> _searchTextUpdatedSubject = new();
	private readonly Entry _searchEntry;
	private readonly List<(int, int)> _keyCodeRanges = new()
	{
		(48, 90),
		(96, 111),
		(186, 222)
	};

	public ApplicationMenuWindow(IObservable<ApplicationMenuViewModel> viewModelObservable)
		: base(WindowType.Toplevel)
	{
		var iconCache = new Dictionary<string, ApplicationMenuAppIcon>();
		var allAppsObservable = viewModelObservable.Select(vm => vm.AllApps).DistinctUntilChanged().UnbundleMany(a => a.Name);
		var displayedAppsObservable = viewModelObservable.Select(vm => vm.AppsToDisplay).DistinctUntilChanged().UnbundleMany(a => a.Name);

		allAppsObservable.Subscribe(file =>
		{
			var appIcon = new ApplicationMenuAppIcon(file);
			appIcon.Halign = Align.Start;
			appIcon.Valign = Align.Start;
			appIcon.Expand = false;

			Observable.FromEventPattern<ButtonReleaseEventArgs>(appIcon, nameof(ButtonReleaseEvent))
				.TakeUntilDestroyed(appIcon)
				.Where(static e => e.EventArgs.Event.Button == 1)
				.WithLatestFrom(file)
				.Subscribe(t => _appLaunch.OnNext(t.Second));

			appIcon.ContextMenuRequested
				.TakeUntilDestroyed(appIcon)
				.Subscribe(f => _contextMenuRequested.OnNext(f));

			iconCache.Add(file.Key, appIcon);
			file.Subscribe(_ => { }, _ => { }, () => iconCache.Remove(file.Key));
		});

		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = false;
		TypeHint = WindowTypeHint.Dialog;
		Visual = Screen.RgbaVisual;
		AppPaintable = true;
		Visible = false;

		SetSizeRequest(640, 725);

		_hiddenEntry = new Entry();
		_hiddenEntry.IsEditable = false;

		_searchEntry = new Entry("");
		_searchEntry.Expand = true;
		_searchEntry.IsEditable = true;
		_searchEntry.Valign = Align.Center;
		_searchEntry.StyleContext.AddClass("app-menu__search-input");
		_searchEntry.HeightRequest = 30;
		_searchEntry.Halign = Align.Fill;
		_searchEntry.PrimaryIconStock = Stock.Find;
		_searchEntry.PlaceholderText = "Search all applications";

		Observable.Return("")
			.Merge(Observable.FromEventPattern(_searchEntry, nameof(_searchEntry.TextInserted)).Select(_ => _searchEntry.Text))
			.Merge(Observable.FromEventPattern(_searchEntry, nameof(_searchEntry.TextDeleted)).Select(_ => _searchEntry.Text))
			.TakeUntilDestroyed(this)
			.DistinctUntilChanged()
			.Subscribe(s => _searchTextUpdatedSubject.OnNext(s));

		var label = new Label("Pinned");
		label.Halign = Align.Start;
		label.Valign = Align.End;
		label.MarginStart = 58;
		label.MarginBottom = 16;
		label.Expand = true;
		label.StyleContext.AddClass("app-menu__label-pinned");

		viewModelObservable
			.Select(s => s.SearchText)
			.Where(s => s != null)
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.Subscribe(s => label.Text = s.Length > 0 ? "Search results" : "Pinned");

		var pinnedAppsGrid = new FlowBox();
		pinnedAppsGrid.MarginStart = 32;
		pinnedAppsGrid.MarginEnd = 32;
		pinnedAppsGrid.RowSpacing = 0;
		pinnedAppsGrid.ColumnSpacing = 0;
		pinnedAppsGrid.MaxChildrenPerLine = 6;
		pinnedAppsGrid.MinChildrenPerLine = 6;
		pinnedAppsGrid.SelectionMode = SelectionMode.None;
		pinnedAppsGrid.Orientation = Orientation.Horizontal;
		pinnedAppsGrid.Homogeneous = false;
		pinnedAppsGrid.Valign = Align.Start;
		pinnedAppsGrid.Halign = Align.Start;
		pinnedAppsGrid.ForEach(displayedAppsObservable, i => iconCache[i.Key]);

		var pinnedAppsScrolledWindow = new ScrolledWindow();
		pinnedAppsScrolledWindow.Add(pinnedAppsGrid);
		pinnedAppsScrolledWindow.Expand = true;
		pinnedAppsScrolledWindow.MarginBottom = 128;

		var actionBar = new Box(Orientation.Horizontal, 0);
		actionBar.Expand = true;
		actionBar.StyleContext.AddClass("app-menu__action-bar");
		actionBar.Add(new Label("Test") { Expand = true });

		var layout = new Grid();
		layout.Expand = true;
		layout.ColumnHomogeneous = true;
		layout.Attach(_searchEntry, 2, 0, 4, 1);
		layout.Attach(_hiddenEntry, 1, 0, 1, 1);
		layout.Attach(label, 1, 1, 6, 1);
		layout.Attach(pinnedAppsScrolledWindow, 1, 2, 6, 8);
		layout.Attach(actionBar, 1, 10, 6, 1);
		layout.StyleContext.AddClass("app-menu__window");

		Add(layout);
		ShowAll();
		Hide();
		_hiddenEntry.Hide();
	}

	public IObservable<string> SearchTextUpdated => _searchTextUpdatedSubject;
	public IObservable<DesktopFile> AppLaunch => _appLaunch;
	public IObservable<DesktopFile> ContextMenuRequested => _contextMenuRequested;

	[ConnectBefore]
	protected override bool OnKeyPressEvent(EventKey evnt)
	{
		if (evnt.Key == Key.Escape)
		{
			Visible = false;
			return true;
		}

		if (!_searchEntry.HasFocus && _keyCodeRanges.Any(r => evnt.KeyValue >= r.Item1 && evnt.KeyValue <= r.Item2))
		{
			_searchEntry.GrabFocusWithoutSelecting();
		}

		return base.OnKeyPressEvent(evnt);
	}

	public void ClosePopup()
	{
		_searchEntry.Text = "";
		_hiddenEntry.GrabFocus();
		Hide();
	}

	public void Popup()
	{
		Window.SetShadowWidth(0, 0, 0, 0);
		Show();
	}
}
