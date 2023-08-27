using System.Collections.Immutable;
using System.Reactive.Linq;
using Fluxor;
using GtkNetPanel.Services;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.State;

public class RootStateSelectors
{
	public IObservable<RootState> RootState { get; }
	public IObservable<ApplicationMenuState> ApplicationMenuState { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedAppBar { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedAppMenu { get; }
	public IObservable<ImmutableList<DesktopFile>> ValidDesktopFiles { get; }
	public IObservable<string> SearchText { get; }

	public RootStateSelectors(IState<RootState> rootState)
	{
		RootState = rootState
			.ToObservable()
			.DistinctUntilChanged();

		PinnedAppBar = RootState
			.DistinctUntilChanged()
			.Select(s => s.Groups)
			.DistinctUntilChanged()
			.Select(g => g.Where(a => a.IsPinnedToApplicationBar).Select(g => g.DesktopFile).ToImmutableList())
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y));

		PinnedAppMenu = RootState
			.DistinctUntilChanged()
			.Select(s => s.ApplicationMenuState)
			.DistinctUntilChanged()
			.Select(s => s.PinnedDesktopFiles)
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y));

		ValidDesktopFiles = RootState
			.Select(s => s.DesktopFiles)
			.DistinctUntilChanged()
			.Select(s => s.OrderBy(f => f.Name).Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec)).ToImmutableList());

		ApplicationMenuState = RootState
			.Select(s => s.ApplicationMenuState)
			.DistinctUntilChanged();

		SearchText = ApplicationMenuState
			.Select(s => s.SearchText)
			.DistinctUntilChanged();
	}
}