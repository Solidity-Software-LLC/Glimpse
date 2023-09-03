using System.Collections.Immutable;
using System.Reactive.Linq;
using Fluxor;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public class RootStateSelectors
{
	public IObservable<RootState> RootState { get; }
	public IObservable<StartMenuState> StartMenuState { get; }
	public IObservable<TaskbarState> TaskbarState { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedTaskbarApps { get; }
	public IObservable<ImmutableList<DesktopFile>> PinnedStartMenuApps { get; }
	public IObservable<ImmutableList<DesktopFile>> AllDesktopFiles { get; }
	public IObservable<string> SearchText { get; }
	public IObservable<string> PowerButtonCommand { get; }
	public IObservable<string> SettingsButtonCommand { get; }
	public IObservable<string> UserSettingsCommand { get; }
	public IObservable<string> VolumeCommand { get; }
	public IObservable<string> UserIconPath { get; }
	public IObservable<ImmutableList<TaskState>> Tasks { get; }

	public RootStateSelectors(IState<RootState> rootState)
	{
		RootState = rootState
			.ToObservable()
			.DistinctUntilChanged();

		TaskbarState = RootState
			.Select(s => s.TaskbarState)
			.DistinctUntilChanged();

		Tasks = TaskbarState
			.Select(s => s.Tasks)
			.DistinctUntilChanged();

		PinnedTaskbarApps = TaskbarState
			.Select(s => s.PinnedDesktopFiles)
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y));

		PinnedStartMenuApps = RootState
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

		PowerButtonCommand = StartMenuState
			.Select(s => s.PowerButtonCommand)
			.DistinctUntilChanged();

		SettingsButtonCommand = StartMenuState
			.Select(s => s.SettingsButtonCommand)
			.DistinctUntilChanged();

		UserSettingsCommand = StartMenuState
			.Select(s => s.UserSettingsCommand)
			.DistinctUntilChanged();

		VolumeCommand = RootState
			.Select(s => s.VolumeCommand)
			.DistinctUntilChanged();

		UserIconPath = RootState
			.Select(s => s.UserState)
			.DistinctUntilChanged()
			.Select(s => s.IconPath)
			.DistinctUntilChanged();
	}
}
