using System.Collections.Immutable;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarViewModel
{
	public ImmutableDictionary<string, TaskState> Tasks { get; set; } = ImmutableDictionary<string, TaskState>.Empty;
	public TaskState ShownWindowPicker { get; set; }
}
