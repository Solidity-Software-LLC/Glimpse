using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using GLib;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.FreeDesktop;
using Gtk;
using Key = Gdk.Key;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.Components.StartMenu;

public class StartMenuWindow : Window
{
	private readonly Entry _hiddenEntry;
	private readonly Subject<DesktopFile> _appLaunch = new();
	private readonly Subject<DesktopFile> _contextMenuRequested = new();
	private readonly Entry _searchEntry;
	private readonly FlowBox _pinnedAppsGrid;
	private readonly List<(int, int)> _keyCodeRanges = new()
	{
		(48, 90),
		(96, 111),
		(186, 222)
	};

	public StartMenuWindow(IObservable<StartMenuViewModel> viewModelObservable)
		: base(WindowType.Toplevel)
	{
		var iconCache = new Dictionary<string, StartMenuAppIcon>();
		var allAppsObservable = viewModelObservable.Select(vm => vm.AllApps).DistinctUntilChanged().UnbundleMany(a => a.IniFile.FilePath).RemoveIndex();
		var displayedAppsObservable = viewModelObservable.Select(vm => vm.AppsToDisplay).DistinctUntilChanged().UnbundleMany(a => a.IniFile.FilePath).RemoveIndex();

		allAppsObservable.Subscribe(file =>
		{
			var appIcon = new StartMenuAppIcon(file);
			appIcon.Halign = Align.Start;
			appIcon.Valign = Align.Start;
			appIcon.Expand = false;

			appIcon.ObserveButtonRelease()
				.Where(static e => e.Event.Button == 1)
				.WithLatestFrom(file)
				.Subscribe(t => _appLaunch.OnNext(t.Second));

			appIcon.ContextMenuRequested
				.TakeUntilDestroyed(appIcon)
				.Subscribe(f => _contextMenuRequested.OnNext(f));

			iconCache.Add(file.Key, appIcon);
			file.Subscribe(f => appIcon.Data["DesktopFile"] = f, static _ => { }, () => iconCache.Remove(file.Key));
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

		_hiddenEntry = new Entry();
		_hiddenEntry.IsEditable = false;

		_searchEntry = new Entry("");
		_searchEntry.Expand = true;
		_searchEntry.IsEditable = true;
		_searchEntry.Valign = Align.Center;
		_searchEntry.AddClass("start-menu__search-input");
		_searchEntry.Halign = Align.Fill;
		_searchEntry.PrimaryIconStock = Stock.Find;
		_searchEntry.PlaceholderText = "Search all applications";

		SearchTextUpdated = Observable.Return("")
			.Merge(Observable.FromEventPattern(_searchEntry, nameof(_searchEntry.TextInserted)).Select(_ => _searchEntry.Text))
			.Merge(Observable.FromEventPattern(_searchEntry, nameof(_searchEntry.TextDeleted)).Select(_ => _searchEntry.Text))
			.TakeUntilDestroyed(this)
			.DistinctUntilChanged();

		var label = new Label("Pinned");
		label.Halign = Align.Start;
		label.Valign = Align.End;
		label.MarginStart = 58;
		label.MarginBottom = 16;
		label.Expand = true;
		label.StyleContext.AddClass("start-menu__label-pinned");

		viewModelObservable
			.Select(s => s.SearchText)
			.Where(s => s != null)
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.Subscribe(s => label.Text = s.Length > 0 ? "Search results" : "Pinned");

		_pinnedAppsGrid = new FlowBox();
		_pinnedAppsGrid.MarginStart = 32;
		_pinnedAppsGrid.MarginEnd = 32;
		_pinnedAppsGrid.RowSpacing = 4;
		_pinnedAppsGrid.ColumnSpacing = 4;
		_pinnedAppsGrid.MaxChildrenPerLine = 6;
		_pinnedAppsGrid.MinChildrenPerLine = 6;
		_pinnedAppsGrid.SelectionMode = SelectionMode.Single;
		_pinnedAppsGrid.Orientation = Orientation.Horizontal;
		_pinnedAppsGrid.Homogeneous = false;
		_pinnedAppsGrid.Valign = Align.Start;
		_pinnedAppsGrid.Halign = Align.Start;
		_pinnedAppsGrid.ActivateOnSingleClick = true;
		_pinnedAppsGrid.ForEach(displayedAppsObservable, i => iconCache[i.Key]);

		viewModelObservable
			.Select(vm => vm.AppsToDisplay)
			.DistinctUntilChanged()
			.Subscribe(_ => _pinnedAppsGrid.InvalidateSort());

		viewModelObservable
			.Select(vm => vm.SearchText)
			.DistinctUntilChanged()
			.WithLatestFrom(viewModelObservable)
			.Subscribe(t => _pinnedAppsGrid.SortFunc = string.IsNullOrEmpty(t.First) ? (c1, c2) => PinnedSort(c1, c2, t.Second.PinnedStartApps) : AlphabeticalSort);

		_pinnedAppsGrid.ObserveEvent<ChildActivatedArgs>(nameof(_pinnedAppsGrid.ChildActivated))
			.Select(e => e.Child.Child as StartMenuAppIcon)
			.Subscribe(appIcon => _appLaunch.OnNext(appIcon.Data["DesktopFile"] as DesktopFile));

		_searchEntry.ObserveEvent<KeyReleaseEventArgs>(nameof(KeyReleaseEvent))
			.Where(e => e.Event.Key == Key.Return || e.Event.Key == Key.KP_Enter)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.AppsToDisplay).DistinctUntilChanged())
			.Where(t => t.Second.Any())
			.Subscribe(t => _appLaunch.OnNext(t.Second.FirstOrDefault()));

		var pinnedAppsScrolledWindow = new ScrolledWindow();
		pinnedAppsScrolledWindow.Add(_pinnedAppsGrid);
		pinnedAppsScrolledWindow.Expand = true;
		pinnedAppsScrolledWindow.MarginBottom = 128;

		var layout = new Grid();
		layout.Expand = true;
		layout.ColumnHomogeneous = true;
		layout.Attach(_searchEntry, 1, 0, 6, 1);
		layout.Attach(_hiddenEntry, 1, 0, 1, 1);
		layout.Attach(label, 1, 1, 6, 1);
		layout.Attach(pinnedAppsScrolledWindow, 1, 2, 6, 8);
		layout.Attach(CreateActionBar(viewModelObservable.Select(vm => vm.ActionBarViewModel).DistinctUntilChanged()), 1, 10, 6, 1);
		layout.StyleContext.AddClass("start-menu__window");

		Add(layout);
		ShowAll();
		Hide();
		_hiddenEntry.Hide();
	}

	private int PinnedSort(FlowBoxChild child1, FlowBoxChild child2, ImmutableList<DesktopFile> pinnedStartApps)
	{
		var first = child1.Child as StartMenuAppIcon;
		var second = child2.Child as StartMenuAppIcon;
		var firstDesktopFile = first.Data["DesktopFile"] as DesktopFile;
		var secondDesktopFile = second.Data["DesktopFile"] as DesktopFile;
		return pinnedStartApps.IndexOf(firstDesktopFile).CompareTo(pinnedStartApps.IndexOf(secondDesktopFile));
	}

	private int AlphabeticalSort(FlowBoxChild child1, FlowBoxChild child2)
	{
		var first = child1.Child as StartMenuAppIcon;
		var second = child2.Child as StartMenuAppIcon;
		var firstDesktopFile = first.Data["DesktopFile"] as DesktopFile;
		var secondDesktopFile = second.Data["DesktopFile"] as DesktopFile;
		return firstDesktopFile.Name.CompareTo(secondDesktopFile.Name);
	}

	private Widget CreateActionBar(IObservable<ActionBarViewModel> viewModel)
	{
		var userImage = new Image().AddClass("start-menu__account-icon");

		viewModel
			.Select(vm => vm.UserIconPath)
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.Select(path => string.IsNullOrEmpty(path) ? Assets.Person.ScaleSimple(42, 42, InterpType.Bilinear) : new Pixbuf(path))
			.Select(p => p.ScaleSimple(42, 42, InterpType.Bilinear))
			.Subscribe(p => userImage.Pixbuf = p);

		var userButton = new Button()
			.AddClass("start-menu__user-settings-button").AddMany(
				new Box(Orientation.Horizontal, 0).AddMany(
					userImage,
					new Label(Environment.UserName).AddClass("start-menu__username")));

		userButton.Valign = Align.Center;
		UserSettingsClicked = userButton.ObserveButtonRelease();

		var settingsButton = new Button(new Image(Assets.Settings.ScaleSimple(24, 24, InterpType.Bilinear)));
		settingsButton.AddClass("start-menu__settings");
		settingsButton.Valign = Align.Center;
		settingsButton.Halign = Align.End;
		SettingsButtonClicked = settingsButton.ObserveButtonRelease();

		var powerButton = new Button(new Image(Assets.Power.ScaleSimple(24, 24, InterpType.Bilinear)));
		powerButton.AddClass("start-menu__power");
		powerButton.Valign = Align.Center;
		powerButton.Halign = Align.End;
		PowerButtonClicked = powerButton.ObserveButtonRelease();

		var actionBar = new Box(Orientation.Horizontal, 0);
		actionBar.Expand = true;
		actionBar.AddClass("start-menu__action-bar");
		actionBar.AddMany(userButton, new Label(Environment.MachineName) { Expand = true }, settingsButton, powerButton);
		return actionBar;
	}

	public IObservable<string> SearchTextUpdated { get; }
	public IObservable<DesktopFile> AppLaunch => _appLaunch;
	public IObservable<DesktopFile> ContextMenuRequested => _contextMenuRequested;
	public IObservable<ButtonReleaseEventArgs> PowerButtonClicked { get; private set; }
	public IObservable<ButtonReleaseEventArgs> SettingsButtonClicked { get; private set; }
	public IObservable<ButtonReleaseEventArgs> UserSettingsClicked { get; private set; }

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
		Hide();
	}

	public void Popup()
	{
		_searchEntry.Text = "";
		_hiddenEntry.GrabFocus();
		_pinnedAppsGrid.UnselectAll();
		Show();
	}
}
