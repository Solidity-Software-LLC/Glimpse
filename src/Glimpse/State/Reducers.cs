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
	public static RootState ReduceAddTaskAction(RootState state, AddTaskAction action)
	{
		return state with { TaskbarState = state.TaskbarState with { Tasks = state.TaskbarState.Tasks.Add(action.Task) } };
	}

	[ReducerMethod]
	public static RootState ReduceRemoveTaskAction(RootState state, RemoveTaskAction action)
	{
		var taskToRemove = state.TaskbarState.Tasks.FirstOrDefault(t => t.WindowRef.Id == action.WindowId);
		if (taskToRemove == null) return state;
		return state with { TaskbarState = state.TaskbarState with { Tasks = state.TaskbarState.Tasks.Remove(taskToRemove) } };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateFocusAction(RootState state, UpdateFocusAction action)
	{
		return state with { FocusedWindow = action.WindowRef };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateStartMenuSearchTextAction(RootState state, UpdateStartMenuSearchTextAction action)
	{
		return state with { StartMenuState = state.StartMenuState with { SearchText = action.SearchText } };
	}
}
