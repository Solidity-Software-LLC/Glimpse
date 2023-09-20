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

public class StartMenuAppViewModel
{
	public int Index { get; set; }
	public DesktopFile DesktopFile { get; set; }
	public bool IsVisible { get; set; }
	public bool IsPinnedToStartMenu { get; set; }
	public bool IsPinnedToTaskbar { get; set; }
}

public class StartMenuViewModel
{
	public ImmutableList<StartMenuAppViewModel> AllApps { get; set; }
	public string SearchText { get; set; }
	public ActionBarViewModel ActionBarViewModel { get; set; }
}
