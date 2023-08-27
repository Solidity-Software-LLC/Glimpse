using System.Collections.Immutable;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuViewModel
{
	public ImmutableList<DesktopFile> AllApps { get; set; }
	public ImmutableList<DesktopFile> PinnedApps { get; set; }
	public string SearchText { get; set; }
	public ImmutableList<DesktopFile> AppsToDisplay { get; set; }
	public ImmutableList<DesktopFile> PinnedTaskbarApps { get; set; }
}
