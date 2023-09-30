using System.Collections.Immutable;
using System.Reactive.Linq;
using Fluxor;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public class RootStateSelectors
{
	public IObservable<RootState> RootState { get; }
	public IObservable<TaskbarState> TaskbarState { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedTaskbarApps { get; }
	public IObservable<ImmutableList<DesktopFile>> AllDesktopFiles { get; }
	public IObservable<string> VolumeCommand { get; }
	public IObservable<string> UserIconPath { get; }
	public IObservable<ImmutableList<TaskGroup>> Groups { get; }
	public IObservable<ImmutableDictionary<IWindowRef,BitmapImage>> Screenshots { get; set; }

	public RootStateSelectors(IState<RootState> rootState)
	{
		RootState = rootState
			.ToObservable()
			.DistinctUntilChanged();

		TaskbarState = RootState
			.Select(s => s.TaskbarState)
			.DistinctUntilChanged();

		PinnedTaskbarApps = TaskbarState
			.Select(s => s.PinnedDesktopFiles)
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y));

		AllDesktopFiles = RootState
			.Select(s => s.DesktopFiles)
			.DistinctUntilChanged()
			.Select(s => s.OrderBy(f => f.Name).Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec)).ToImmutableList());

		VolumeCommand = RootState
			.Select(s => s.VolumeCommand)
			.DistinctUntilChanged();

		UserIconPath = RootState
			.Select(s => s.UserState)
			.DistinctUntilChanged()
			.Select(s => s.IconPath)
			.DistinctUntilChanged();

		Groups = RootState
			.Select(s => s.Groups)
			.DistinctUntilChanged();

		Screenshots = RootState
			.Select(s => s.Screenshots)
			.DistinctUntilChanged();
	}
}
