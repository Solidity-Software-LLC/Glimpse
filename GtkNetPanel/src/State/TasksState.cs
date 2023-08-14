using System.Collections.Immutable;
using Fluxor;
using GtkNetPanel.Services;

namespace GtkNetPanel.State;

[FeatureState]
public class TasksState
{
	public ImmutableDictionary<string, TaskState> Tasks = ImmutableDictionary<string, TaskState>.Empty;
}

public class TaskState
{
	public string Title { get; set; }
	public List<string> State { get; set; }
	public List<BitmapImage> Icons { get; set; }
	public GenericWindowRef WindowRef { get; set; }
	public string ApplicationName { get; set; }
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

public class TasksStateReducers
{
	[ReducerMethod]
	public static TasksState ReduceAddTaskAction(TasksState state, AddTaskAction action)
	{
		return new TasksState() { Tasks = state.Tasks.SetItem(action.Task.WindowRef.Id, action.Task) };
	}

	[ReducerMethod]
	public static TasksState ReduceRemoveTaskAction(TasksState state, RemoveTaskAction action)
	{
		if (state.Tasks.ContainsKey(action.WindowId))
		{
			return new TasksState() { Tasks = state.Tasks.Remove(action.WindowId) };
		}

		return state;
	}
}
