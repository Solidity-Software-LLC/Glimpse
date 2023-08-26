using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.Services.GtkSharp;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuWindow : Window
{
	private const int NumColumns = 6;

	private readonly Entry _hiddenEntry;
	private readonly Subject<DesktopFile> _appLaunch = new();
	private readonly Entry _searchEntry;
	private readonly List<(int, int)> _keyCodeRanges = new()
	{
		(48, 90),
		(96, 111),
		(186, 222)
	};

	public ApplicationMenuWindow(IObservable<ApplicationMenuViewModel> viewModelObservable) : base(WindowType.Toplevel)
	{
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = false;
		TypeHint = WindowTypeHint.Dialog;
		Visual = Screen.RgbaVisual;
		StyleContext.AddClass("app-menu__window");
		Visible = false;

		SetSizeRequest(640, 725);

		_hiddenEntry = new Entry();
		_hiddenEntry.IsEditable = false;

		_searchEntry = new Entry();
		_searchEntry.Expand = true;
		_searchEntry.IsEditable = true;
		_searchEntry.Valign = Align.Center;
		_searchEntry.StyleContext.AddClass("app-menu__search-input");
		_searchEntry.HeightRequest = 30;
		_searchEntry.Halign = Align.Fill;
		_searchEntry.PrimaryIconStock = Stock.Find;
		_searchEntry.PlaceholderText = "Search all applications";

		var pinnedLabel = new Label("Pinned");
		pinnedLabel.Halign = Align.Start;
		pinnedLabel.Valign = Align.End;
		pinnedLabel.MarginStart = 58;
		pinnedLabel.MarginBottom = 16;
		pinnedLabel.Expand = true;
		pinnedLabel.StyleContext.AddClass("app-menu__label-pinned");

		var pinnedAppsGrid = new Grid();
		pinnedAppsGrid.MarginStart = 32;
		pinnedAppsGrid.MarginEnd = 32;
		pinnedAppsGrid.RowSpacing = 16;

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
		layout.Attach(pinnedLabel, 1, 1, 6, 1);
		layout.Attach(pinnedAppsScrolledWindow, 1, 2, 6, 8);
		layout.Attach(actionBar, 1, 10, 6, 1);

		Add(layout);

		var appsObservable = viewModelObservable
			.Select(v => v.DesktopFiles)
			.DistinctUntilChanged()
			.Select(files => files
				.OrderBy(f => f.Name)
				.Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec))
				.Select(i => (i, CreateAppIcon(i)))
				.ToList())
			.Publish();

		var searchEntryUpdated = Observable.Return(new EventPattern<EventArgs>(null, EventArgs.Empty))
			.Merge(Observable.FromEventPattern<EventArgs>(_searchEntry, nameof(_searchEntry.TextInserted)))
			.Merge(Observable.FromEventPattern<EventArgs>(_searchEntry, nameof(_searchEntry.TextDeleted)))
			.Select(_ => _searchEntry.Text);

		var pinnedFilesObservable = viewModelObservable
			.Select(vm => vm.PinnedFiles)
			.DistinctUntilChanged()
			.CombineLatest(appsObservable)
			.Select(t => t.First.Select(pinnedFile => t.Second.First(app => app.i == pinnedFile)).ToList());

		appsObservable
			.CombineLatest(pinnedFilesObservable, searchEntryUpdated)
			.Select(t => string.IsNullOrEmpty(t.Third)
				? t.Second
				: t.First.Where(d => d.i.Name.Contains(t.Third, StringComparison.InvariantCultureIgnoreCase)).ToList())
			.Subscribe(filteredApps =>
			{
				pinnedAppsGrid.RemoveAllChildren();
				pinnedAppsGrid.AutoPopulateGrid(filteredApps.Select(t => t.Item2), NumColumns);
				pinnedAppsGrid.ShowAll();
			});

		appsObservable.Connect();

		FocusOutEvent += (_, _) =>
		{
			_hiddenEntry.GrabFocus();
			Visible = false;
		};
	}

	public IObservable<DesktopFile> AppLaunch => _appLaunch;

	private ApplicationMenuAppIcon CreateAppIcon(DesktopFile desktopFile)
	{
		var appIcon = new ApplicationMenuAppIcon(desktopFile);

		Observable.FromEventPattern<ButtonReleaseEventArgs>(appIcon, nameof(ButtonReleaseEvent))
				.TakeUntil(Observable.FromEventPattern<EventArgs>(appIcon, nameof(appIcon.Destroyed)))
				.Where(e => e.EventArgs.Event.Button == 1)
				.Subscribe(t => _appLaunch.OnNext(desktopFile));

		return appIcon;
	}

	[ConnectBefore]
	protected override bool OnKeyPressEvent(EventKey evnt)
	{
		if (evnt.Key == Gdk.Key.Escape)
		{
			_hiddenEntry.GrabFocus();
			_searchEntry.Text = "";
			return true;
		}

		if (!_searchEntry.HasFocus && _keyCodeRanges.Any(r => evnt.KeyValue >= r.Item1 && evnt.KeyValue <= r.Item2))
		{
			_searchEntry.GrabFocusWithoutSelecting();
		}

		return base.OnKeyPressEvent(evnt);
	}

	public void Popup()
	{
		Visible = true;
		ShowAll();
		_hiddenEntry.Hide();
	}
}
