using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GLib;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.StartMenu;

public class StartMenuSelectors
{
	public IObservable<StartMenuViewModel> ViewModel { get; }

	public StartMenuSelectors(RootStateSelectors rootStateSelectors)
	{
		var appsToDisplayObservable = rootStateSelectors.SearchText
			.CombineLatest(
				rootStateSelectors.PinnedStartMenuApps,
				rootStateSelectors.AllDesktopFiles)
			.Select(t => string.IsNullOrEmpty(t.First) ? t.Second
				: t.Third.Where(d => d.Name.Contains(t.First, StringComparison.InvariantCultureIgnoreCase)).ToImmutableList());

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
