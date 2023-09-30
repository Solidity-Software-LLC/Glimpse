using System.Collections.Immutable;
using Fluxor.Selectors;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public static class RootStateSelectors
{
	public static ISelector<RootState> RootState => SelectorFactory.CreateFeatureSelector<RootState>();
	public static ISelector<TaskbarState> TaskbarState => SelectorFactory.CreateSelector(RootState, s => s.TaskbarState);
	public static ISelector<ImmutableList<DesktopFile>> PinnedTaskbarApps => SelectorFactory.CreateSelector(TaskbarState, s => s.PinnedDesktopFiles);
	public static ISelector<ImmutableList<DesktopFile>> DesktopFiles => SelectorFactory.CreateSelector(RootState, s => s.DesktopFiles);

	public static ISelector<ImmutableList<DesktopFile>> AllDesktopFiles =>
		SelectorFactory.CreateSelector(DesktopFiles, s => s
			.OrderBy(f => f.Name)
			.Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec))
			.ToImmutableList());

	public static ISelector<string> VolumeCommand => SelectorFactory.CreateSelector(RootState, s => s.VolumeCommand);
	public static ISelector<UserState> UserState => SelectorFactory.CreateSelector(RootState, s => s.UserState);
	public static ISelector<string> UserIconPath => SelectorFactory.CreateSelector(UserState, s => s.IconPath);
	public static ISelector<ImmutableList<TaskGroup>> Groups => SelectorFactory.CreateSelector(RootState, s => s.Groups);
	public static ISelector<ImmutableDictionary<IWindowRef, BitmapImage>> Screenshots => SelectorFactory.CreateSelector(RootState, s => s.Screenshots);
}
