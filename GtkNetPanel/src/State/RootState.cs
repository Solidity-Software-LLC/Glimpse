using System.Collections.Immutable;
using Fluxor;
using GtkNetPanel.Services;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.State;

[FeatureState]
public record RootState
{
	public ImmutableList<ApplicationGroupState> Groups = ImmutableList<ApplicationGroupState>.Empty;
	public GenericWindowRef FocusedWindow = new();
	public ImmutableList<DesktopFile> DesktopFiles = ImmutableList<DesktopFile>.Empty;
	public ImmutableList<DesktopFile> AppMenuPinnedDesktopFiles = ImmutableList<DesktopFile>.Empty;

	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
}

public record ApplicationGroupState
{
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;
	public DesktopFile DesktopFile { get; set; }
	public string ApplicationName { get; set; }
	public bool IsPinnedToApplicationBar { get; set; }
	public bool IsPinnedToApplicationMenu { get; set; }

	public virtual bool Equals(ApplicationGroupState other) => ReferenceEquals(this, other);

	public ApplicationGroupState(DesktopFile file)
	{
		ApplicationName = file.Name;
		DesktopFile = file;
		IsPinnedToApplicationBar = true;
	}

	public ApplicationGroupState(TaskState task)
	{
		ApplicationName = task.ApplicationName;
		DesktopFile = task.DesktopFile;
		Tasks = Tasks.Add(task);
		IsPinnedToApplicationBar = false;
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

public class AddAppBarPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class AddAppMenuPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class TogglePinningAction
{
	public string ApplicationName { get; set; }
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
	public static RootState ReduceTogglePinningAction(RootState state, TogglePinningAction action)
	{
		var group = state.Groups.FirstOrDefault(g => g.ApplicationName == action.ApplicationName);
		if (group == null) return state;
		var newGroup = group with { IsPinnedToApplicationBar = !group.IsPinnedToApplicationBar };
		return state with { Groups = state.Groups.Replace(group, newGroup) };
	}

	[ReducerMethod]
	public static RootState ReduceAddAppMenuPinnedDesktopFileAction(RootState state, AddAppMenuPinnedDesktopFileAction action)
	{
		return state with { AppMenuPinnedDesktopFiles = state.AppMenuPinnedDesktopFiles.Add(action.DesktopFile) };
	}

	[ReducerMethod]
	public static RootState ReducerAddAppBarPinnedDesktopFileAction(RootState state, AddAppBarPinnedDesktopFileAction action)
	{
		var groups = state.Groups;
		var groupToReplace = groups.FirstOrDefault(t => t.ApplicationName == action.DesktopFile.Name);

		if (groupToReplace == null)
		{
			return state with { Groups = groups.Add(new ApplicationGroupState(action.DesktopFile)) };
		}

		return state;
	}

	[ReducerMethod]
	public static RootState ReduceAddTaskAction(RootState state, AddTaskAction action)
	{
		var groups = state.Groups;
		var groupToReplace = groups.FirstOrDefault(t => t.ApplicationName == action.Task.ApplicationName);

		if (groupToReplace == null)
		{
			return state with { Groups = groups.Add(new ApplicationGroupState(action.Task)) };
		}

		var updatedGroup = groupToReplace with { Tasks = groupToReplace.Tasks.Add(action.Task) };
		return state with { Groups = groups.Replace(groupToReplace, updatedGroup) };
	}

	[ReducerMethod]
	public static RootState ReduceRemoveTaskAction(RootState state, RemoveTaskAction action)
	{
		var group = state.Groups.FirstOrDefault(g => g.Tasks.Any(t => t.WindowRef.Id == action.WindowId));

		if (group == null) return state;

		if (group.Tasks.Count == 1 && !group.IsPinnedToApplicationBar)
		{
			return state with { Groups = state.Groups.Remove(group) };
		}

		var taskToRemove = group.Tasks.FirstOrDefault(t => t.WindowRef.Id == action.WindowId);
		var updatedGroup = group with { Tasks = group.Tasks.Remove(taskToRemove) };
		return state with { Groups = state.Groups.Replace(group, updatedGroup) };
	}

	[ReducerMethod]
	public static RootState ReduceUpdateFocusAction(RootState state, UpdateFocusAction action)
	{
		return state with { FocusedWindow = action.WindowRef };
	}
}
