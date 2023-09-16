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
		var appsToDisplayObservable = rootStateSelectors.SearchText.Select(s => s?.ToLower())
			.CombineLatest(
				rootStateSelectors.PinnedStartMenuApps,
				rootStateSelectors.AllDesktopFiles)
			.Select(t =>
			{
				var (searchText, pinnedApps, allDesktopFiles) = t;

				return string.IsNullOrEmpty(searchText)
					? pinnedApps
					: allDesktopFiles.Where(d => searchText.AllCharactersIn(d.Name.ToLower())).ToImmutableList();
			});

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

		ViewModel = rootStateSelectors.PinnedStartMenuApps
			.CombineLatest(
				rootStateSelectors.AllDesktopFiles,
				rootStateSelectors.SearchText,
				appsToDisplayObservable,
				rootStateSelectors.PinnedTaskbarApps,
				actionBarViewModelSelector)
			.Select(t => new StartMenuViewModel()
			{
				PinnedStartApps = t.First,
				AllApps = t.Second,
				SearchText = t.Third,
				AppsToDisplay = t.Fourth,
				PinnedTaskbarApps = t.Fifth,
				ActionBarViewModel = t.Sixth
			})
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));
	}
}
