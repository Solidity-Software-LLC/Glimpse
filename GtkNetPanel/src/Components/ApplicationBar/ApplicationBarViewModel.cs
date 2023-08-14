using System.Collections.Immutable;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationBar;

public record ApplicationBarViewModel
{
	public ImmutableDictionary<string, IconGroupViewModel> Groups { get; set; } = ImmutableDictionary<string, IconGroupViewModel>.Empty;
	public string GroupForWindowPicker { get; set; }

	public virtual bool Equals(ApplicationBarViewModel other) => ReferenceEquals(this, other);
}

public record IconGroupViewModel
{
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;
	public string ApplicationName { get; set; }

	public virtual bool Equals(IconGroupViewModel other) => ReferenceEquals(this, other);

	public IconGroupViewModel(TaskState taskState)
	{
		ApplicationName = taskState.ApplicationName;
		Tasks = Tasks.Add(taskState);
	}

	public IconGroupViewModel UpdateTask(TaskState task)
	{
		var newGroup = this;
		var newGroupTasks = newGroup.Tasks;
		var indexToUpdate = newGroupTasks.FindIndex(t => t.WindowRef.Id == task.WindowRef.Id);
		return indexToUpdate == -1 ? newGroup with { Tasks = newGroupTasks.Add(task) } : newGroup with { Tasks = newGroupTasks.SetItem(indexToUpdate, task) };
	}
}
