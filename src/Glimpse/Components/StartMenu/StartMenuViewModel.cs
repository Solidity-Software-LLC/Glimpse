using System.Collections.Immutable;
using Gdk;
using Glimpse.Services.Configuration;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;

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
	public Pixbuf Icon { get; set; }
	public bool IsVisible { get; set; }
	public bool IsPinnedToStartMenu { get; set; }
	public bool IsPinnedToTaskbar { get; set; }
	public Dictionary<string, Pixbuf> ActionIcons { get; set; }
}

public class StartMenuViewModel
{
	public ImmutableList<StartMenuAppViewModel> AllApps { get; set; }
	public string SearchText { get; set; }
	public bool DisableDragAndDrop { get; set; }
	public ActionBarViewModel ActionBarViewModel { get; set; }
	public ImmutableDictionary<StartMenuChips, StartMenuAppFilteringChip> Chips { get; set; }
	public ImmutableList<StartMenuLaunchIconContextMenuItem> LaunchIconContextMenu { get; set; }
}
