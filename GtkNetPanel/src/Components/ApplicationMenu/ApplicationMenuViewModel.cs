using System.Collections.Immutable;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuViewModel
{
	public ImmutableList<DesktopFile> DesktopFiles { get; set; }
}
