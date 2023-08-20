using System.Collections.Immutable;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationBar;

public record ApplicationBarViewModel
{
	public ImmutableList<ApplicationBarGroupViewModel> Groups = ImmutableList<ApplicationBarGroupViewModel>.Empty;
}

public record ApplicationBarGroupViewModel
{
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;
	public string ApplicationName { get; set; }
	public DesktopFile DesktopFile { get; set; }

	public virtual bool Equals(ApplicationBarGroupViewModel other) => ReferenceEquals(this, other);
}
