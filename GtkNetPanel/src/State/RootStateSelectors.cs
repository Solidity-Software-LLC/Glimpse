using System.Collections.Immutable;
using System.Reactive.Linq;
using Fluxor;
using GtkNetPanel.Services;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.State;

public class RootStateSelectors
{
	public IObservable<RootState> RootState { get; }
	public IObservable<StartMenuState> StartMenuState { get; }
	public IObservable<TaskbarState> TaskbarState { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedTaskbarApps { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedStartMenuApps { get; }
	public IObservable<ImmutableList<DesktopFile>> AllDesktopFiles { get; }
	public IObservable<string> SearchText { get; }

	public RootStateSelectors(IState<RootState> rootState)
	{
		RootState = rootState
			.ToObservable()
			.DistinctUntilChanged();

		TaskbarState = RootState
			.DistinctUntilChanged()
			.Select(s => s.TaskbarState)
			.DistinctUntilChanged();

		PinnedTaskbarApps = TaskbarState
			.Select(s => s.PinnedDesktopFiles)
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y));

		PinnedStartMenuApps = RootState
			.DistinctUntilChanged()
			.Select(s => s.StartMenuState)
			.DistinctUntilChanged()
			.Select(s => s.PinnedDesktopFiles)
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y));

		AllDesktopFiles = RootState
			.Select(s => s.DesktopFiles)
			.DistinctUntilChanged()
			.Select(s => s.OrderBy(f => f.Name).Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec)).ToImmutableList());

		StartMenuState = RootState
			.Select(s => s.StartMenuState)
			.DistinctUntilChanged();

		SearchText = StartMenuState
			.Select(s => s.SearchText)
			.DistinctUntilChanged();
	}
}
