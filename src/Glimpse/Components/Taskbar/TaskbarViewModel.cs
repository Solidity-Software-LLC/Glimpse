using System.Collections.Immutable;
using Gdk;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.Components.Taskbar;

public record TaskbarViewModel
{
	public ImmutableList<TaskbarGroupViewModel> Groups { get; init; } = ImmutableList<TaskbarGroupViewModel>.Empty;
}

public class TaskState
{
	public string Title { get; init; }
	public Pixbuf Icon { get; init; }
	public IWindowRef WindowRef { get; init; }
	public AllowedWindowActions[] AllowedActions { get; init; }
	public Pixbuf Screenshot { get; init; }
}

public class TaskbarGroupContextMenuViewModel
{
	public bool IsPinned { get; init; }
	public Dictionary<string,Pixbuf> ActionIcons { get; set; }
	public DesktopFile DesktopFile { get; init; }
	public Pixbuf LaunchIcon { get; set; }
	public bool CanClose { get; set; }
}

public record TaskbarGroupViewModel
{
	public ImmutableList<TaskState> Tasks { get; init; } = ImmutableList<TaskState>.Empty;
	public string Id { get; init; }
	public DesktopFile DesktopFile { get; init; }
	public bool DemandsAttention { get; init; }
	public Pixbuf Icon { get; init; }
	public TaskbarGroupContextMenuViewModel ContextMenu { get; set; }

	public virtual bool Equals(TaskbarGroupViewModel other) => ReferenceEquals(this, other);
}
