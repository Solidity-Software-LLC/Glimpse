using System.Collections.Immutable;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationBar;

public record ApplicationBarViewModel
{
	public ImmutableDictionary<string, IconGroupViewModel> Groups { get; set; } = ImmutableDictionary<string, IconGroupViewModel>.Empty;
	public string GroupForWindowPicker { get; set; }

	public virtual bool Equals(ApplicationBarViewModel other) => ReferenceEquals(this, other);

	public ApplicationBarViewModel UpdateTaskInGroup(string applicationName, TaskState task)
	{
		if (!Groups.ContainsKey(applicationName))
		{
			return this with { Groups = Groups.Add(applicationName, new IconGroupViewModel(task)) };
		}

		return this with { Groups = Groups.SetItem(applicationName, Groups[applicationName].UpdateTask(task)) };
	}

	public ApplicationBarViewModel RemoveTaskFromGroup(string applicationName, string taskId)
	{
		var group = Groups[applicationName].RemoveTask(taskId);

		if (group.Tasks.Count == 0)
		{
			return this with { Groups = Groups.Remove(applicationName) };
		}

		return this with { Groups = Groups.SetItem(applicationName, group) };
	}
}

public record IconGroupViewModel
{
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;
	public string ApplicationName { get; set; }
	public DesktopFile DesktopFile { get; set; }

	public virtual bool Equals(IconGroupViewModel other) => ReferenceEquals(this, other);

	public IconGroupViewModel(TaskState taskState)
	{
		ApplicationName = taskState.ApplicationName;
		Tasks = Tasks.Add(taskState);
		DesktopFile = taskState.DesktopFile;
	}

	public IconGroupViewModel UpdateTask(TaskState task)
	{
		var newGroup = this;
		var newGroupTasks = newGroup.Tasks;
		var indexToUpdate = newGroupTasks.FindIndex(t => t.WindowRef.Id == task.WindowRef.Id);
		return indexToUpdate == -1 ? newGroup with { Tasks = newGroupTasks.Add(task) } : newGroup with { Tasks = newGroupTasks.SetItem(indexToUpdate, task) };
	}

	public IconGroupViewModel RemoveTask(string taskId)
	{
		var newGroup = this;
		var newGroupTasks = newGroup.Tasks;
		var taskToRemove = newGroupTasks.FirstOrDefault(t => t.WindowRef.Id == taskId);
		return taskToRemove == null ? newGroup : newGroup with { Tasks = newGroupTasks.Remove(taskToRemove) };
	}
}
