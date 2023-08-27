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
			.CombineLatest(rootStateSelectors.PinnedAppMenu, rootStateSelectors.ValidDesktopFiles)
			.Select(t => string.IsNullOrEmpty(t.First) ? t.Second : t.Third.Where(d => d.Name.Contains(t.First, StringComparison.InvariantCultureIgnoreCase)).ToImmutableList())
			.Do(_ => { }, e => Console.WriteLine(e));

		ViewModel = rootStateSelectors.PinnedAppMenu
			.CombineLatest(
				rootStateSelectors.ValidDesktopFiles,
				rootStateSelectors.SearchText,
				appsToDisplayObservable,
				rootStateSelectors.PinnedAppBar)
			.Select(t => new StartMenuViewModel()
			{
				PinnedStartApps = t.First,
				AllApps = t.Second,
				SearchText = t.Third,
				AppsToDisplay = t.Fourth,
				PinnedTaskbarApps = t.Fifth
			})
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));
	}
}
