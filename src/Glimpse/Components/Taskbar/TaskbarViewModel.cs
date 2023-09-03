using System.Collections.Immutable;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public record TaskbarViewModel
{
	public ImmutableList<TaskbarGroupViewModel> Groups = ImmutableList<TaskbarGroupViewModel>.Empty;
}

public record TaskbarGroupViewModel
{
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;
	public string ApplicationName { get; set; }
	public DesktopFile DesktopFile { get; set; }
	public bool IsPinned { get; set; }

	public virtual bool Equals(TaskbarGroupViewModel other) => ReferenceEquals(this, other);
}
