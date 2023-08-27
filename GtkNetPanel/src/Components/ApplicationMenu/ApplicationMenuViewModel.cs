using System.Collections.Immutable;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuViewModel
{
	public ImmutableList<DesktopFile> DesktopFiles { get; set; }
	public ImmutableList<DesktopFile> PinnedFiles { get; set; }
	public string SearchText { get; set; }
	public ImmutableList<DesktopFile> AppsToDisplay { get; set; }
}
