using System.Collections.Immutable;
using Fluxor;
using Gdk;

namespace GtkNetPanel.State;

[FeatureState]
public class TasksState
{
	public ImmutableList<TaskState> Tasks = ImmutableList<TaskState>.Empty;
}

public class TaskState
{
	public string Name { get; set; }
	public List<Atom> State { get; set; }
	public List<WindowIcon> Icons { get; set; }
	public int ProcessId { get; set; }
}

public class WindowIcon
{
	public int Width { get; set; }
	public int Height { get; set; }
	public byte[] Data { get; set; }
}

public class AddTaskAction
{
	public TaskState Task { get; set; }
}

public class TasksStateReducers
{
	[ReducerMethod]
	public static TasksState ReduceAddTaskAction(TasksState state, AddTaskAction action)
	{
		return new TasksState() { Tasks = state.Tasks.Add(action.Task) };
	}
}
