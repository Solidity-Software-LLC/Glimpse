using System.Collections.Immutable;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public record TaskbarViewModel
{
	public ImmutableList<ApplicationBarGroupViewModel> Groups = ImmutableList<ApplicationBarGroupViewModel>.Empty;
}

public record ApplicationBarGroupViewModel
{
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;
	public string ApplicationName { get; set; }
	public DesktopFile DesktopFile { get; set; }
	public bool IsPinned { get; set; }

	public virtual bool Equals(ApplicationBarGroupViewModel other) => ReferenceEquals(this, other);
}
