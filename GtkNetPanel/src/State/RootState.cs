using System.Collections.Immutable;
using Fluxor;
using GtkNetPanel.Services;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.State;

[FeatureState]
public record RootState
{
	public TaskbarState TaskbarState { get; set; } = new();
	public GenericWindowRef FocusedWindow = new();
	public ImmutableList<DesktopFile> DesktopFiles = ImmutableList<DesktopFile>.Empty;
	public StartMenuState StartMenuState { get; set; } = new();

	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
}

public record TaskbarState
{
	public ImmutableList<TaskbarGroupState> Groups { get; set; } = ImmutableList<TaskbarGroupState>.Empty;
	public ImmutableList<DesktopFile> PinnedDesktopFiles { get; set; } = ImmutableList<DesktopFile>.Empty;

	public virtual bool Equals(TaskbarState other) => ReferenceEquals(this, other);
}

public record StartMenuState
{
	public ImmutableList<DesktopFile> PinnedDesktopFiles = ImmutableList<DesktopFile>.Empty;
	public string SearchText { get; set; }

	public virtual bool Equals(StartMenuState other) => ReferenceEquals(this, other);
}

public record TaskbarGroupState
{
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;
	public DesktopFile DesktopFile { get; set; }
	public string ApplicationName { get; set; }

	public virtual bool Equals(TaskbarGroupState other) => ReferenceEquals(this, other);

	public TaskbarGroupState(DesktopFile file)
	{
		ApplicationName = file.Name;
		DesktopFile = file;
	}

	public TaskbarGroupState(TaskState task)
	{
		ApplicationName = task.ApplicationName;
		DesktopFile = task.DesktopFile;
		Tasks = Tasks.Add(task);
	}
}

public class TaskState
{
	public string Title { get; set; }
	public List<string> State { get; set; }
	public List<BitmapImage> Icons { get; set; }
	public GenericWindowRef WindowRef { get; set; }
	public string ApplicationName { get; set; }
	public DesktopFile DesktopFile { get; set; }
	public AllowedWindowActions[] AllowedActions { get; set; }
	public BitmapImage Screenshot { get; set; }
}

public class BitmapImage
{
	public int Width { get; set; }
	public int Height { get; set; }
	public byte[] Data { get; set; }
}

public class AddTaskAction
{
	public TaskState Task { get; set; }
}

public class RemoveTaskAction
{
	public string WindowId { get; set; }
}

public class UpdateFocusAction
{
	public GenericWindowRef WindowRef { get; set; }
}

public class AddTaskbarPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class AddStartMenuPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class UpdateStartMenuSearchTextAction
{
	public string SearchText { get; set; }
}

public class ToggleTaskbarPinningAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class ToggleStartMenuPinningAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class UpdateDesktopFilesAction
{
	public ImmutableList<DesktopFile> DesktopFiles { get; set; }
}

public class TasksStateReducers
{
	[ReducerMethod]
	public static RootState ReduceUpdateDesktopFilesAction(RootState state, UpdateDesktopFilesAction action)
	{
		return state with { DesktopFiles = action.DesktopFiles };
	}

	[ReducerMethod]
	public static RootState ReduceAddTaskbarPinnedDesktopFileAction(RootState state, AddTaskbarPinnedDesktopFileAction action)
	{
		var groups = state.TaskbarState.Groups;
		var matchingGroup = groups.FirstOrDefault(g => g.DesktopFile.IniConfiguration.FilePath == action.DesktopFile.IniConfiguration.FilePath);
		var newState = state with { TaskbarState = state.TaskbarState with { PinnedDesktopFiles = state.TaskbarState.PinnedDesktopFiles.Add(action.DesktopFile), } };

		if (matchingGroup == null)
		{
			newState = newState with { TaskbarState = newState.TaskbarState with { Groups = groups.Add(new TaskbarGroupState(action.DesktopFile)) } };
		}

		return newState;
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
		var desktopFileToRemove = pinnedApps.FirstOrDefault(a => a.IniConfiguration.FilePath == action.DesktopFile.IniConfiguration.FilePath);

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
		var desktopFileToRemove = pinnedApps.FirstOrDefault(a => a.IniConfiguration.FilePath == action.DesktopFile.IniConfiguration.FilePath);

		if (desktopFileToRemove != null)
		{
			return state with { StartMenuState = state.StartMenuState with { PinnedDesktopFiles = pinnedApps.Remove(desktopFileToRemove) } };
		}

		return state with { StartMenuState = state.StartMenuState with { PinnedDesktopFiles = pinnedApps.Add(action.DesktopFile) } };
	}

	[ReducerMethod]
	public static RootState ReduceAddTaskAction(RootState state, AddTaskAction action)
	{
		var groups = state.TaskbarState.Groups;
		var groupToReplace = groups.FirstOrDefault(t => t.ApplicationName == action.Task.ApplicationName);

		if (groupToReplace == null)
		{
			return state with { TaskbarState = state.TaskbarState with { Groups = groups.Add(new TaskbarGroupState(action.Task)) } };
		}

		var updatedGroup = groupToReplace with { Tasks = groupToReplace.Tasks.Add(action.Task) };
		return state with { TaskbarState = state.TaskbarState with { Groups = groups.Replace(groupToReplace, updatedGroup) } };
	}

	[ReducerMethod]
	public static RootState ReduceRemoveTaskAction(RootState state, RemoveTaskAction action)
	{
		var group = state.TaskbarState.Groups.FirstOrDefault(g => g.Tasks.Any(t => t.WindowRef.Id == action.WindowId));

		if (group == null) return state;

		var isPinned = state.TaskbarState.PinnedDesktopFiles.Any(f => f.IniConfiguration.FilePath == group.DesktopFile.IniConfiguration.FilePath);

		if (group.Tasks.Count == 1 && !isPinned)
		{
			return state with { TaskbarState = state.TaskbarState with { Groups = state.TaskbarState.Groups.Remove(group) } };
		}

		var taskToRemove = group.Tasks.FirstOrDefault(t => t.WindowRef.Id == action.WindowId);
		var updatedGroup = group with { Tasks = group.Tasks.Remove(taskToRemove) };
		return state with { TaskbarState = state.TaskbarState with { Groups = state.TaskbarState.Groups.Replace(group, updatedGroup) } };
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
