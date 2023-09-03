using System.Collections.Immutable;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.Components.StartMenu;

public class ActionBarViewModel
{
	public string SettingsButtonCommand { get; set; }
	public string UserSettingsCommand { get; set; }
	public string UserIconPath { get; set; }
	public string PowerButtonCommand { get; set; }
}

public class StartMenuViewModel
{
	public ImmutableList<DesktopFile> AllApps { get; set; }
	public ImmutableList<DesktopFile> PinnedStartApps { get; set; }
	public string SearchText { get; set; }
	public ActionBarViewModel ActionBarViewModel { get; set; }
	public ImmutableList<DesktopFile> AppsToDisplay { get; set; }
	public ImmutableList<DesktopFile> PinnedTaskbarApps { get; set; }
}
