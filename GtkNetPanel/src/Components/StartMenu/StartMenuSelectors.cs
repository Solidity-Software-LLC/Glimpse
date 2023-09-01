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

		ViewModel = rootStateSelectors.PinnedStartMenuApps
			.CombineLatest(
				rootStateSelectors.AllDesktopFiles,
				rootStateSelectors.SearchText,
				appsToDisplayObservable,
				rootStateSelectors.PinnedTaskbarApps,
				rootStateSelectors.PowerButtonCommand,
				rootStateSelectors.SettingsButtonCommand,
				rootStateSelectors.UserSettingsCommand)
			.Select(t => new StartMenuViewModel()
			{
				PinnedStartApps = t.First,
				AllApps = t.Second,
				SearchText = t.Third,
				AppsToDisplay = t.Fourth,
				PinnedTaskbarApps = t.Fifth,
				PowerButtonCommand = t.Sixth,
				SettingsButtonCommand = t.Seventh,
				UserSettingsCommand = t.Eighth
			})
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));
	}
}
