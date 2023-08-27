using System.Collections.Immutable;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.Components.StartMenu;

public class StartMenuViewModel
{
	public ImmutableList<DesktopFile> AllApps { get; set; }
	public ImmutableList<DesktopFile> PinnedStartApps { get; set; }
	public string SearchText { get; set; }
	public ImmutableList<DesktopFile> AppsToDisplay { get; set; }
	public ImmutableList<DesktopFile> PinnedTaskbarApps { get; set; }
}
