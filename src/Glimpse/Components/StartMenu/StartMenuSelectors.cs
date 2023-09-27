using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GLib;
using Glimpse.Extensions;
using Glimpse.State;

namespace Glimpse.Components.StartMenu;

public class StartMenuSelectors
{
	public IObservable<StartMenuViewModel> ViewModel { get; }

	public StartMenuSelectors(RootStateSelectors rootStateSelectors)
	{
		var actionBarViewModelSelector = rootStateSelectors.PowerButtonCommand
			.CombineLatest(
				rootStateSelectors.SettingsButtonCommand,
				rootStateSelectors.UserSettingsCommand,
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
				rootStateSelectors.SearchText,
				rootStateSelectors.PinnedTaskbarApps,
				rootStateSelectors.PinnedStartMenuApps)
			.Select(t =>
			{
				var (allDesktopFiles, searchText, pinnedTaskbarApps, pinnedStartMenuApps) = t;
				var results = new LinkedList<StartMenuAppViewModel>();
				var index = 0;
				var isSearching = !string.IsNullOrEmpty(searchText);
				var lowerCaseSearchText = searchText.ToLower();

				foreach (var f in allDesktopFiles)
				{
					var pinnedIndex = pinnedStartMenuApps.FindIndex(a => a.IniFile.FilePath == f.IniFile.FilePath);
					var taskbarIndex = pinnedTaskbarApps.FindIndex(a => a.IniFile.FilePath == f.IniFile.FilePath);
					var isVisible = isSearching ? lowerCaseSearchText.AllCharactersIn(f.Name.ToLower()) : pinnedIndex != -1;

					var appViewModel = new StartMenuAppViewModel();
					appViewModel.DesktopFile = f;
					appViewModel.IsPinnedToTaskbar = taskbarIndex != -1;
					appViewModel.IsPinnedToStartMenu = pinnedIndex != -1;
					appViewModel.IsVisible = isVisible;
					appViewModel.Index = isSearching && isVisible ? index++ : pinnedIndex;
					results.AddLast(appViewModel);
				}

				return results.OrderBy(r => r.Index).ToImmutableList();
			});

		ViewModel = allAppsSelector
			.CombineLatest(rootStateSelectors.SearchText, actionBarViewModelSelector)
			.Select(t => new StartMenuViewModel()
			{
				AllApps = t.First,
				SearchText = t.Second,
				DisableDragAndDrop = t.Second.Length > 0,
				ActionBarViewModel = t.Third
			})
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));
	}
}
