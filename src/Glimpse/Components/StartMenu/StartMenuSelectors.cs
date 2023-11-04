using System.Collections.Immutable;
using Fluxor.Selectors;
using Glimpse.Extensions;
using Glimpse.Services.Configuration;
using Glimpse.State;

namespace Glimpse.Components.StartMenu;

public static class StartMenuSelectors
{
	private static readonly ISelector<string> s_searchText = SelectorFactory.CreateSelector(RootStateSelectors.StartMenuState, s => s.SearchText);
	private static readonly ISelector<string> s_powerButtonCommand = SelectorFactory.CreateSelector(RootStateSelectors.Configuration, s => s.PowerButtonCommand);
	private static readonly ISelector<string> s_settingsButtonCommand = SelectorFactory.CreateSelector(RootStateSelectors.Configuration, s => s.SettingsButtonCommand);
	private static readonly ISelector<string> s_taskManagerCommand = SelectorFactory.CreateSelector(RootStateSelectors.Configuration, s => s.TaskManagerCommand);
	private static readonly ISelector<string> s_userSettingsCommand = SelectorFactory.CreateSelector(RootStateSelectors.Configuration, s => s.UserSettingsCommand);
	private static readonly ISelector<ImmutableDictionary<StartMenuChips, StartMenuAppFilteringChip>> s_chips = SelectorFactory.CreateSelector(RootStateSelectors.StartMenuState, s => s.Chips);

	private static readonly ISelector<ActionBarViewModel> s_actionBarViewModelSelector = SelectorFactory.CreateSelector(
		s_powerButtonCommand,
		s_settingsButtonCommand,
		s_userSettingsCommand,
		RootStateSelectors.UserIconPath,
		(powerButtonCommand, settingsButtonCommand, userSettingsCommand, userIconPath) => new ActionBarViewModel()
		{
			PowerButtonCommand = powerButtonCommand,
			SettingsButtonCommand = settingsButtonCommand,
			UserSettingsCommand = userSettingsCommand,
			UserIconPath = userIconPath
		});

	private static readonly ISelector<ImmutableList<StartMenuAppViewModel>> s_allAppsSelector = SelectorFactory.CreateSelector(
		RootStateSelectors.AllDesktopFiles,
		s_searchText,
		RootStateSelectors.Configuration,
		s_chips,
		RootStateSelectors.NamedIcons,
		(allDesktopFiles, searchText, configuration, chips, desktopFileIcons) =>
		{
			var results = new LinkedList<StartMenuAppViewModel>();
			var index = 0;
			var isShowingSearchResults = chips[StartMenuChips.SearchResults].IsSelected;
			var isShowingPinned = chips[StartMenuChips.Pinned].IsSelected;
			var isShowingAllApps = chips[StartMenuChips.AllApps].IsSelected;
			var lowerCaseSearchText = searchText.ToLower();

			foreach (var f in allDesktopFiles)
			{
				var pinnedIndex = configuration.StartMenu.PinnedLaunchers.IndexOf(f.IniFile.FilePath);
				var taskbarIndex = configuration.Taskbar.PinnedLaunchers.IndexOf(f.IniFile.FilePath);
				var isSearchMatch = isShowingSearchResults && lowerCaseSearchText.AllCharactersIn(f.Name.ToLower());
				var isPinned = pinnedIndex > -1;
				var isVisible = isShowingAllApps || (isShowingSearchResults && isSearchMatch) || (isShowingPinned && isPinned);
				var appIcon = desktopFileIcons.ById.TryGetValue(f.IconName, out var i) ? i : Assets.MissingImage;

				var appViewModel = new StartMenuAppViewModel();
				appViewModel.DesktopFile = f;
				appViewModel.Icon = appIcon;
				appViewModel.IsPinnedToTaskbar = taskbarIndex > -1;
				appViewModel.IsPinnedToStartMenu = pinnedIndex > -1;
				appViewModel.IsVisible = isVisible;
				appViewModel.Index = (isShowingSearchResults || isShowingAllApps) && isVisible ? index++ : pinnedIndex;
				appViewModel.ActionIcons = f.Actions.ToDictionary(
					a => a.ActionName,
					a => string.IsNullOrEmpty(a.IconName) ? appIcon
						: desktopFileIcons.ById.TryGetValue(a.IconName, out var i) ? i
						: Assets.MissingImage);

				results.AddLast(appViewModel);
			}

			return results.OrderBy(r => r.Index).ToImmutableList();
		});

	private static readonly ISelector<ImmutableList<StartMenuLaunchIconContextMenuItem>> s_menuItems = SelectorFactory.CreateSelector(
		RootStateSelectors.StartMenuLaunchIconContextMenuItems,
		s_powerButtonCommand,
		s_settingsButtonCommand,
		s_taskManagerCommand,
		(menuItems, powerButtonCommand, allSettingsCommands, taskManagerCommand) => menuItems
			.Add(new() { DisplayText = "separator" })
			.Add(new() { DisplayText = "Glimpse config", Executable = "xdg-open", Arguments = ConfigurationFile.FilePath })
			.Add(new() { DisplayText = "Settings", Executable = allSettingsCommands })
			.Add(new() { DisplayText = "Task Manager", Executable = taskManagerCommand })
			.Add(new() { DisplayText = "separator" })
			.Add(new() { DisplayText = "Shutdown or sign out", Executable = powerButtonCommand }));

	public static readonly ISelector<StartMenuViewModel> ViewModel = SelectorFactory.CreateSelector(
		s_allAppsSelector,
		s_searchText,
		s_actionBarViewModelSelector,
		s_chips,
		s_menuItems,
		(allApps, searchText, actionBarViewModel, chips, menuItems) =>
		{
			return new StartMenuViewModel()
			{
				AllApps = allApps,
				SearchText = searchText,
				DisableDragAndDrop = searchText.Length > 0,
				ActionBarViewModel = actionBarViewModel,
				Chips = chips,
				LaunchIconContextMenu = menuItems
			};
		});
}
