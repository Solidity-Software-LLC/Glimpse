using Fluxor;

namespace Glimpse.State;

public class Reducers
{
	[ReducerMethod]
	public static RootState ReduceUpdateVolumeCommandAction(RootState state, UpdateVolumeCommandAction action)
	{
		return state with { VolumeCommand = action.Command };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateUserAction(RootState state, UpdateUserAction action)
	{
		return state with { UserState = state.UserState with { UserName = action.UserName, IconPath = action.IconPath } };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateUserSettingsCommandAction(RootState state, UpdateUserSettingsCommandAction action)
	{
		return state with { StartMenuState = state.StartMenuState with {  UserSettingsCommand = action.Command } };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateSettingsButtonCommandAction(RootState state, UpdateSettingsButtonCommandAction action)
	{
		return state with { StartMenuState = state.StartMenuState with { SettingsButtonCommand = action.Command } };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateDesktopFilesAction(RootState state, UpdatePowerButtonCommandAction action)
	{
		return state with { StartMenuState = state.StartMenuState with { PowerButtonCommand = action.Command } };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateDesktopFilesAction(RootState state, UpdateDesktopFilesAction action)
	{
		return state with { DesktopFiles = action.DesktopFiles };
	}

	[ReducerMethod]
	public static RootState ReduceAddTaskbarPinnedDesktopFileAction(RootState state, AddTaskbarPinnedDesktopFileAction action)
	{
		return state with { TaskbarState = state.TaskbarState with { PinnedDesktopFiles = state.TaskbarState.PinnedDesktopFiles.Add(action.DesktopFile) } };
	}

	[ReducerMethod]
	public static RootState ReduceAddStartMenuPinnedDesktopFileAction(RootState state, AddStartMenuPinnedDesktopFileAction action)
	{
		return state with
		{
			StartMenuState = state.StartMenuState with
			{
				PinnedDesktopFiles = state.StartMenuState.PinnedDesktopFiles.Add(action.DesktopFile)
			}
		};
	}

	[ReducerMethod]
	public static RootState ReduceToggleTaskbarPinningAction(RootState state, ToggleTaskbarPinningAction action)
	{
		var pinnedApps = state.TaskbarState.PinnedDesktopFiles;
		var desktopFileToRemove = pinnedApps.FirstOrDefault(a => a.IniFile.FilePath == action.DesktopFile.IniFile.FilePath);

		if (desktopFileToRemove != null)
		{
			return state with { TaskbarState = state.TaskbarState with { PinnedDesktopFiles = pinnedApps.Remove(desktopFileToRemove) } };
		}

		return state with { TaskbarState = state.TaskbarState with { PinnedDesktopFiles = pinnedApps.Add(action.DesktopFile) } };
	}

	[ReducerMethod]
	public static RootState ReduceToggleStartMenuPinningAction(RootState state, ToggleStartMenuPinningAction action)
	{
		var pinnedApps = state.StartMenuState.PinnedDesktopFiles;
		var desktopFileToRemove = pinnedApps.FirstOrDefault(a => a.IniFile.FilePath == action.DesktopFile.IniFile.FilePath);

		if (desktopFileToRemove != null)
		{
			return state with { StartMenuState = state.StartMenuState with { PinnedDesktopFiles = pinnedApps.Remove(desktopFileToRemove) } };
		}

		return state with { StartMenuState = state.StartMenuState with { PinnedDesktopFiles = pinnedApps.Add(action.DesktopFile) } };
	}

	[ReducerMethod]
	public static RootState ReduceRemoveWindowAction(RootState state, RemoveWindowAction action)
	{
		var taskToRemove = state.Windows.FirstOrDefault(t => t.WindowRef.Id == action.WindowProperties.WindowRef.Id);
		if (taskToRemove == null) return state;
		return state with { Windows = state.Windows.Remove(taskToRemove) };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateWindowAction(RootState state, UpdateWindowAction action)
	{
		var windowToReplace = state.Windows.FirstOrDefault(t => t.WindowRef.Id == action.WindowProperties.WindowRef.Id);

		if (windowToReplace == null)
		{
			return state with { Windows = state.Windows.Add(action.WindowProperties) };
		}

		return state with { Windows = state.Windows.Replace(windowToReplace, action.WindowProperties) };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateStartMenuSearchTextAction(RootState state, UpdateStartMenuSearchTextAction action)
	{
		return state with { StartMenuState = state.StartMenuState with { SearchText = action.SearchText } };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateScreenshotsAction(RootState state, UpdateScreenshotsAction action)
	{
		var updated = state.Screenshots;
		foreach (var w in action.Screenshots) updated = updated.SetItem(w.Window, w.Screenshot);
		return state with { Screenshots = updated };
	}
}
