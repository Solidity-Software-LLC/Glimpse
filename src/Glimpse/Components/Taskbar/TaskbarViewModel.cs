using System.Collections.Immutable;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public record TaskbarViewModel
{
	public ImmutableList<TaskbarGroupViewModel> Groups { get; init; } = ImmutableList<TaskbarGroupViewModel>.Empty;
}

public record TaskbarGroupViewModel
{
	public ImmutableList<TaskState> Tasks { get; init; } = ImmutableList<TaskState>.Empty;
	public string Id { get; init; }
	public DesktopFile DesktopFile { get; init; }
	public bool IsPinned { get; init; }
	public bool DemandsAttention { get; init; }

	public virtual bool Equals(TaskbarGroupViewModel other) => ReferenceEquals(this, other);
}
