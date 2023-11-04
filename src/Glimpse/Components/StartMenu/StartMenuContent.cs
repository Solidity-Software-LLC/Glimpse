using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using GLib;
using Glimpse.Components.Shared;
using Glimpse.Components.Shared.ForEach;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;
using Key = Gdk.Key;
using Menu = Gtk.Menu;

namespace Glimpse.Components.StartMenu;

public class StartMenuContent : Bin
{
	private readonly Menu _contextMenu;
	private readonly Entry _hiddenEntry;
	private readonly Subject<DesktopFileAction> _runActionSubject = new();
	private readonly Subject<DesktopFile> _appLaunch = new();
	private readonly Subject<string> _toggleTaskbarPinningSubject = new();
	private readonly Subject<string> _toggleStartMenuPinningSubject = new();
	private readonly Entry _searchEntry;
	private readonly ForEachFlowBox<StartMenuAppViewModel, StartMenuAppIcon, string> _apps;
	private readonly List<(int, int)> _keyCodeRanges = new()
	{
		(48, 57),
		(65, 90),
		(97, 122)
	};

	public IObservable<string> SearchTextUpdated { get; }
	public IObservable<DesktopFile> AppLaunch => _appLaunch;
	public IObservable<DesktopFileAction> DesktopFileAction => _runActionSubject;
	public IObservable<ButtonReleaseEventArgs> PowerButtonClicked { get; private set; }
	public IObservable<ButtonReleaseEventArgs> SettingsButtonClicked { get; private set; }
	public IObservable<ButtonReleaseEventArgs> UserSettingsClicked { get; private set; }
	public IObservable<StartMenuChips> ChipActivated { get; private set; }
	public IObservable<(StartMenuAppViewModel, int)> AppOrderingChanged => _apps.OrderingChanged;
	public IObservable<string> ToggleTaskbarPinning => _toggleTaskbarPinningSubject;
	public IObservable<string> ToggleStartMenuPinning => _toggleStartMenuPinningSubject;

	public StartMenuContent(IObservable<StartMenuViewModel> viewModelObservable)
	{
		_contextMenu = new Menu() { ReserveToggleSize = false };

		_hiddenEntry = new Entry();
		_hiddenEntry.IsEditable = false;

		_searchEntry = new Entry("");
		_searchEntry.IsEditable = true;
		_searchEntry.Valign = Align.Center;
		_searchEntry.Halign = Align.Center;
		_searchEntry.PrimaryIconStock = Stock.Find;
		_searchEntry.PlaceholderText = "Search all applications";
		_searchEntry.AddClass("start-menu__search-input");

		SearchTextUpdated = Observable.Return("")
			.Merge(Observable.FromEventPattern(_searchEntry, nameof(_searchEntry.TextInserted)).Select(_ => _searchEntry.Text))
			.Merge(Observable.FromEventPattern(_searchEntry, nameof(_searchEntry.TextDeleted)).Select(_ => _searchEntry.Text))
			.TakeUntilDestroyed(this)
			.Throttle(TimeSpan.FromMilliseconds(50), new SynchronizationContextScheduler(new GLibSynchronizationContext()))
			.DistinctUntilChanged();

		var chipsObs = viewModelObservable.Select(vm => vm.Chips).DistinctUntilChanged();
		var pinnedChip = new Chip("Pinned", chipsObs.Select(c => c[StartMenuChips.Pinned]));
		var allAppsChip = new Chip("All Apps", chipsObs.Select(c => c[StartMenuChips.AllApps]));
		var searchResultsChip = new Chip("Search results", chipsObs.Select(c => c[StartMenuChips.SearchResults]));

		ChipActivated = pinnedChip.ObserveButtonRelease().Select(_ => StartMenuChips.Pinned)
			.Merge(allAppsChip.ObserveButtonRelease().Select(_ => StartMenuChips.AllApps))
			.Merge(searchResultsChip.ObserveButtonRelease().Select(_ => StartMenuChips.SearchResults));

		var chipBox = new Box(Orientation.Horizontal, 4);
		chipBox.Halign = Align.Start;
		chipBox.Add(pinnedChip);
		chipBox.Add(allAppsChip);
		chipBox.Add(searchResultsChip);
		chipBox.AddClass("start-menu__chips");

		_apps = ForEachExtensions.Create(viewModelObservable.Select(vm => vm.AllApps).DistinctUntilChanged(), i => i.DesktopFile.IniFile.FilePath, appObs =>
		{
			var appIcon = new StartMenuAppIcon(appObs);

			appIcon.ContextMenuRequested
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => OpenDesktopFileContextMenu(t.First, t.Second));

			return appIcon;
		});

		_apps.RowSpacing = 0;
		_apps.ColumnSpacing = 0;
		_apps.MaxChildrenPerLine = 6;
		_apps.MinChildrenPerLine = 6;
		_apps.SelectionMode = SelectionMode.Single;
		_apps.Orientation = Orientation.Horizontal;
		_apps.Homogeneous = true;
		_apps.Valign = Align.Start;
		_apps.Halign = Align.Start;
		_apps.ActivateOnSingleClick = true;
		_apps.FilterFunc = c => _apps.GetViewModel(c)?.IsVisible ?? true;
		_apps.AddClass("start-menu__apps");
		_apps.DisableDragAndDrop = viewModelObservable.Select(vm => vm.DisableDragAndDrop).DistinctUntilChanged();

		_apps.ObserveEvent<ChildActivatedArgs>(nameof(_apps.ChildActivated))
			.Select(e => _apps.GetViewModel(e.Child))
			.Subscribe(vm => _appLaunch.OnNext(vm.DesktopFile));

		_searchEntry.ObserveEvent<KeyReleaseEventArgs>(nameof(KeyReleaseEvent))
			.Where(e => e.Event.Key == Key.Return || e.Event.Key == Key.KP_Enter)
			.WithLatestFrom(viewModelObservable.Select(vm => vm.AllApps.Where(a => a.IsVisible)).DistinctUntilChanged())
			.Where(t => t.Second.Any())
			.Subscribe(t => _appLaunch.OnNext(t.Second.FirstOrDefault().DesktopFile));

		var pinnedAppsScrolledWindow = new ScrolledWindow();
		pinnedAppsScrolledWindow.Vexpand = true;
		pinnedAppsScrolledWindow.Add(_apps);
		pinnedAppsScrolledWindow.AddClass("start-menu__apps-scroll-window");

		var layout = new Grid();
		layout.Expand = true;
		layout.ColumnHomogeneous = true;
		layout.Attach(_searchEntry, 1, 0, 6, 1);
		layout.Attach(_hiddenEntry, 1, 0, 1, 1);
		layout.Attach(chipBox, 1, 1, 6, 1);
		layout.Attach(pinnedAppsScrolledWindow, 1, 2, 6, 8);
		layout.Attach(CreateActionBar(viewModelObservable.Select(vm => vm.ActionBarViewModel).DistinctUntilChanged()), 1, 10, 6, 1);
		layout.StyleContext.AddClass("start-menu__window");

		Add(layout);
		ShowAll();
		_hiddenEntry.Hide();
	}

	private Widget CreateActionBar(IObservable<ActionBarViewModel> viewModel)
	{
		var userImage = new Image().AddClass("start-menu__account-icon");

		viewModel
			.Select(vm => vm.UserIconPath)
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.Select(path => string.IsNullOrEmpty(path) || !File.Exists(path) ? Assets.Person.ScaleSimple(42, 42, InterpType.Bilinear) : new Pixbuf(path))
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

	protected override void OnShown()
	{
		_searchEntry.Text = "";
		_hiddenEntry.GrabFocus();
		_apps.UnselectAll();
		base.OnShown();
	}

	private void OpenDesktopFileContextMenu(StartMenuAppViewModel appViewModel, StartMenuViewModel startMenuViewModel)
	{
		var menuItems = ContextMenuHelper.CreateDesktopFileActions(appViewModel.DesktopFile, appViewModel.ActionIcons);

		menuItems.ForEach(m =>
		{
			var action = (DesktopFileAction)m.Data["DesktopFileAction"];
			m.ObserveButtonRelease().Subscribe(_ => _runActionSubject.OnNext(action));
		});

		var isPinnedToStart = startMenuViewModel.AllApps.Any(f => f.IsPinnedToStartMenu && f.DesktopFile == appViewModel.DesktopFile);
		var isPinnedToTaskbar = startMenuViewModel.AllApps.Any(f => f.IsPinnedToTaskbar && f.DesktopFile == appViewModel.DesktopFile);
		var pinStartIcon = isPinnedToStart ? Assets.UnpinIcon : Assets.PinIcon;
		var pinTaskbarIcon = isPinnedToTaskbar ? Assets.UnpinIcon : Assets.PinIcon;
		var pinStart = ContextMenuHelper.CreateMenuItem(isPinnedToStart ? "Unpin from Start" : "Pin to Start", pinStartIcon.Scale(ThemeConstants.MenuItemIconSize));
		pinStart.ObserveButtonRelease().Subscribe(_ => _toggleStartMenuPinningSubject.OnNext(appViewModel.DesktopFile.Id));
		var pinTaskbar = ContextMenuHelper.CreateMenuItem(isPinnedToTaskbar ? "Unpin from taskbar" : "Pin to taskbar", pinTaskbarIcon.Scale(ThemeConstants.MenuItemIconSize));
		pinTaskbar.ObserveButtonRelease().Subscribe(_ => _toggleTaskbarPinningSubject.OnNext(appViewModel.DesktopFile.Id));

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
