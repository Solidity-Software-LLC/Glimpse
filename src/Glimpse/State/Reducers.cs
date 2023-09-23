using System.Collections.Immutable;
using Fluxor;
using Glimpse.Services.FreeDesktop;

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
		var newState = state;
		var group = state.Groups.FirstOrDefault(g => g.Id == action.DesktopFile.IniFile.FilePath);

		if (group == null)
		{
			newState = newState with { Groups = newState.Groups.Add(new TaskGroup() { Id = action.DesktopFile.IniFile.FilePath, DesktopFile = action.DesktopFile }) };
		}

		return newState with { TaskbarState = newState.TaskbarState with { PinnedDesktopFiles = newState.TaskbarState.PinnedDesktopFiles.Add(action.DesktopFile) } };
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
		var newState = state;
		var pinnedApps = state.TaskbarState.PinnedDesktopFiles;
		var desktopFile = pinnedApps.FirstOrDefault(a => a.IniFile.FilePath == action.DesktopFile.IniFile.FilePath);

		if (desktopFile == null)
		{
			return newState with { TaskbarState = newState.TaskbarState with { PinnedDesktopFiles = pinnedApps.Add(action.DesktopFile) } };
		}

		var group = newState.Groups.First(g => g.DesktopFile.IniFile.FilePath == action.DesktopFile.IniFile.FilePath);

		if (!group.Windows.Any())
		{
			newState = newState with { Groups = newState.Groups.Remove(group) };
		}

		return newState with { TaskbarState = newState.TaskbarState with { PinnedDesktopFiles = pinnedApps.Remove(desktopFile) } };
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
		var group = state.Groups.First(g => g.Windows.Any(w => w.WindowRef.Id == action.WindowProperties.WindowRef.Id));

		if (group.Windows.Count == 1 && state.TaskbarState.PinnedDesktopFiles.All(d => d.IniFile.FilePath != group.Id))
		{
			return state with { Groups = state.Groups.Remove(group) };
		}

		var window = group.Windows.First(w => w.WindowRef.Id == action.WindowProperties.WindowRef.Id);
		var newGroup  = group with { Windows = group.Windows.Remove(window) };
		return state with { Groups = state.Groups.Replace(group, newGroup) };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateWindowAction(RootState state, UpdateWindowAction action)
	{
		var group = state.Groups.FirstOrDefault(g => g.Windows.Any(w => w.WindowRef.Id == action.WindowProperties.WindowRef.Id));

		if (group != null)
		{
			var window = group.Windows.First(w => w.WindowRef.Id == action.WindowProperties.WindowRef.Id);
			var updatedGroup  = group with { Windows = group.Windows.Replace(window, action.WindowProperties) };
			return state with { Groups = state.Groups.Replace(group, updatedGroup) };
		}

		var desktopFile = FindAppDesktopFileByName(state.DesktopFiles, action.WindowProperties.ClassHintName)
			?? FindAppDesktopFileByName(state.DesktopFiles, action.WindowProperties.ClassHintClass)
			?? FindAppDesktopFileByName(state.DesktopFiles, action.WindowProperties.Title)
			?? new() { Name = action.WindowProperties.Title, IconName = "application-default-icon", IniFile = new () { FilePath = action.WindowProperties.ClassHintName } };

		group = state.Groups.FirstOrDefault(g => g.Id == desktopFile.IniFile.FilePath);

		if (group == null)
		{
			var newGroup = new TaskGroup() { Id = desktopFile.IniFile.FilePath, Windows = ImmutableList<WindowProperties>.Empty.Add(action.WindowProperties), DesktopFile = desktopFile};
			return state with { Groups = state.Groups.Add(newGroup) };
		}

		return state with { Groups = state.Groups.Replace(group, group with { Windows = group.Windows.Add(action.WindowProperties) }) };
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

	[ReducerMethod]
	public static RootState ReduceUpdateGroupOrderingAction(RootState state, UpdateGroupOrderingAction action)
	{
		if (action.GroupId == null) return state;

		var groupToMove = state.Groups.First(g => g.Id == action.GroupId);
		var newGroups = state.Groups.Remove(groupToMove).Insert(action.NewIndex, groupToMove);
		var newState = state with { Groups = newGroups };

		var pinnedDesktopFiles = newGroups
			.Where(g => state.TaskbarState.PinnedDesktopFiles.Any(f => f.IniFile.FilePath == g.DesktopFile.IniFile.FilePath))
			.Select(g => g.DesktopFile)
			.ToImmutableList();

		var updatedPinnedFiles = pinnedDesktopFiles.Select(f => f.IniFile.FilePath).ToList();
		var existingPinnedFiles = state.TaskbarState.PinnedDesktopFiles.Select(f => f.IniFile.FilePath).ToList();

		if (!updatedPinnedFiles.SequenceEqual(existingPinnedFiles))
		{
			newState = newState with { TaskbarState = newState.TaskbarState with { PinnedDesktopFiles = pinnedDesktopFiles } };
		}

		return newState;
	}

	[ReducerMethod]
	public static RootState ReduceUpdatePinnedAppOrderingAction(RootState state, UpdatePinnedAppOrderingAction action)
	{
		var pinnedAppToMove = state.StartMenuState.PinnedDesktopFiles.First(f => f.IniFile.FilePath == action.DesktopFileKey);
		var newPinnedFiles = state.StartMenuState.PinnedDesktopFiles.Remove(pinnedAppToMove).Insert(action.NewIndex, pinnedAppToMove);
		return state with { StartMenuState = state.StartMenuState with { PinnedDesktopFiles = newPinnedFiles } };
	}

	private static DesktopFile FindAppDesktopFileByName(ImmutableList<DesktopFile> desktopFiles, string applicationName)
	{
		var lowerCaseAppName = applicationName.ToLower();

		return desktopFiles.FirstOrDefault(f => f.Name.ToLower().Contains(lowerCaseAppName))
			?? desktopFiles.FirstOrDefault(f => f.StartupWmClass.ToLower() == lowerCaseAppName)
			?? desktopFiles.FirstOrDefault(f => f.StartupWmClass.ToLower().Contains(lowerCaseAppName))
			?? desktopFiles.FirstOrDefault(f => f.Exec.Executable.ToLower().Contains(lowerCaseAppName))
			?? desktopFiles.FirstOrDefault(f => f.Exec.Executable.ToLower() == lowerCaseAppName);
	}
}
