using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarViewModel
{
	public IEnumerable<TaskState> Tasks { get; set; } = new List<TaskState>();
	public TaskState ShownWindowPicker { get; set; }
}
