using System.Collections.Immutable;
using Fluxor.Selectors;
using Glimpse.Extensions;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;

namespace Glimpse.Components.StartMenu;

public static class StartMenuSelectors
{
	public static ISelector<StartMenuState> StartMenuState => SelectorFactory.CreateFeatureSelector<StartMenuState>();
	public static ISelector<ImmutableList<DesktopFile>> PinnedStartMenuApps => SelectorFactory.CreateSelector(StartMenuState, s => s.PinnedDesktopFiles);
	public static ISelector<string> SearchText => SelectorFactory.CreateSelector(StartMenuState, s => s.SearchText);
	public static ISelector<string> PowerButtonCommand => SelectorFactory.CreateSelector(StartMenuState, s => s.PowerButtonCommand);
	public static ISelector<string> SettingsButtonCommand => SelectorFactory.CreateSelector(StartMenuState, s => s.SettingsButtonCommand);
	public static ISelector<string> UserSettingsCommand => SelectorFactory.CreateSelector(StartMenuState, s => s.UserSettingsCommand);
	public static ISelector<ImmutableDictionary<StartMenuChips, StartMenuAppFilteringChip>> Chips => SelectorFactory.CreateSelector(StartMenuState, s => s.Chips);

	public static ISelector<ActionBarViewModel> ActionBarViewModelSelector => SelectorFactory.CreateSelector(
		PowerButtonCommand,
		SettingsButtonCommand,
		UserSettingsCommand,
		RootStateSelectors.UserIconPath,
		(powerButtonCommand, settingsButtonCommand, userSettingsCommand, userIconPath) => new ActionBarViewModel()
		{
			PowerButtonCommand = powerButtonCommand,
			SettingsButtonCommand = settingsButtonCommand,
			UserSettingsCommand = userSettingsCommand,
			UserIconPath = userIconPath
		});

	public static ISelector<ImmutableList<StartMenuAppViewModel>> AllAppsSelector => SelectorFactory.CreateSelector(
		RootStateSelectors.AllDesktopFiles,
		SearchText,
		RootStateSelectors.PinnedTaskbarApps,
		PinnedStartMenuApps,
		Chips,
		(allDesktopFiles, searchText, pinnedTaskbarApps, pinnedStartMenuApps, chips) =>
		{
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

	public static ISelector<StartMenuViewModel> ViewModel => SelectorFactory.CreateSelector(
		AllAppsSelector,
		SearchText,
		ActionBarViewModelSelector,
		Chips,
		(allApps, searchText, actionBarViewModel, chips) =>
		{
			return new StartMenuViewModel()
			{
				AllApps = allApps,
				SearchText = searchText,
				DisableDragAndDrop = searchText.Length > 0,
				ActionBarViewModel = actionBarViewModel,
				Chips = chips
			};
		});
}
