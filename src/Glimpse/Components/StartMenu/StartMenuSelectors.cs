using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Fluxor;
using GLib;
using Glimpse.Extensions;
using Glimpse.Extensions.Fluxor;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Glimpse.Extensions.Reactive;
using DateTime = System.DateTime;

namespace Glimpse.Components.StartMenu;

public class StartMenuSelectors
{
	public IObservable<StartMenuViewModel> ViewModel { get; }
	public IObservable<StartMenuState> StartMenuState { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedStartMenuApps { get; }
	public IObservable<string> SearchText { get; }
	public IObservable<string> PowerButtonCommand { get; }
	public IObservable<string> SettingsButtonCommand { get; }
	public IObservable<string> UserSettingsCommand { get; }

	public StartMenuSelectors(RootStateSelectors rootStateSelectors, IState<StartMenuState> startMenuState)
	{
		StartMenuState = startMenuState
			.ToObservable()
			.DistinctUntilChanged();

		PinnedStartMenuApps = StartMenuState
			.Select(s => s.PinnedDesktopFiles)
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y));

		SearchText = StartMenuState
			.Select(s => s.SearchText)
			.DistinctUntilChanged();

		PowerButtonCommand = StartMenuState
			.Select(s => s.PowerButtonCommand)
			.DistinctUntilChanged();

		SettingsButtonCommand = StartMenuState
			.Select(s => s.SettingsButtonCommand)
			.DistinctUntilChanged();

		UserSettingsCommand = StartMenuState
			.Select(s => s.UserSettingsCommand)
			.DistinctUntilChanged();

		var chipsSelector = StartMenuState
			.Select(s => s.Chips)
			.DistinctUntilChanged();

		var actionBarViewModelSelector = PowerButtonCommand
			.CombineLatest(
				SettingsButtonCommand,
				UserSettingsCommand,
				rootStateSelectors.UserIconPath)
			.Select(t => new ActionBarViewModel()
			{
				PowerButtonCommand = t.First,
				SettingsButtonCommand = t.Second,
				UserSettingsCommand = t.Third,
				UserIconPath = t.Fourth
			})
			.DistinctUntilChanged();

		var allAppsSelector = rootStateSelectors.AllDesktopFiles
			.CombineLatest(
				SearchText,
				rootStateSelectors.PinnedTaskbarApps,
				PinnedStartMenuApps,
				chipsSelector)
			.Select(t =>
			{
				var (allDesktopFiles, searchText, pinnedTaskbarApps, pinnedStartMenuApps, chips) = t;
				var results = new LinkedList<StartMenuAppViewModel>();
				var index = 0;
				var isShowingSearchResults = chips[StartMenuChips.SearchResults].IsSelected;
				var isShowingPinned = chips[StartMenuChips.Pinned].IsSelected;
				var isShowingAllApps = chips[StartMenuChips.AllApps].IsSelected;
				var lowerCaseSearchText = searchText.ToLower();

				foreach (var f in allDesktopFiles)
				{
					var pinnedIndex = pinnedStartMenuApps.FindIndex(a => a.IniFile.FilePath == f.IniFile.FilePath);
					var taskbarIndex = pinnedTaskbarApps.FindIndex(a => a.IniFile.FilePath == f.IniFile.FilePath);
					var isSearchMatch = isShowingSearchResults && lowerCaseSearchText.AllCharactersIn(f.Name.ToLower());
					var isPinned = pinnedIndex != -1;
					var isVisible = isShowingAllApps || (isShowingSearchResults && isSearchMatch) || (isShowingPinned && isPinned);

					var appViewModel = new StartMenuAppViewModel();
					appViewModel.DesktopFile = f;
					appViewModel.IsPinnedToTaskbar = taskbarIndex != -1;
					appViewModel.IsPinnedToStartMenu = pinnedIndex != -1;
					appViewModel.IsVisible = isVisible;
					appViewModel.Index = (isShowingSearchResults || isShowingAllApps) && isVisible ? index++ : pinnedIndex;
					results.AddLast(appViewModel);
				}

				return results.OrderBy(r => r.Index).ToImmutableList();
			});

		var viewModelObs = allAppsSelector
			.CombineLatest(
				SearchText,
				actionBarViewModelSelector,
				chipsSelector)
			.Select(t => new StartMenuViewModel()
			{
				AllApps = t.First,
				SearchText = t.Second,
				DisableDragAndDrop = t.Second.Length > 0,
				ActionBarViewModel = t.Third,
				Chips = t.Fourth
			})
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Publish();

		ViewModel = viewModelObs;
		viewModelObs.Connect();
	}
}
