using System.Collections.Immutable;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.State;

public class AddTaskAction
{
	public TaskState Task { get; set; }
}

public class RemoveTaskAction
{
	public string WindowId { get; set; }
}

public class UpdateFocusAction
{
	public GenericWindowRef WindowRef { get; set; }
}

public class AddTaskbarPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class AddStartMenuPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class UpdateStartMenuSearchTextAction
{
	public string SearchText { get; set; }
}

public class ToggleTaskbarPinningAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class ToggleStartMenuPinningAction
{
	public DesktopFile DesktopFile { get; set; }
}

public class UpdateDesktopFilesAction
{
	public ImmutableList<DesktopFile> DesktopFiles { get; set; }
}

public class UpdatePowerButtonCommandAction
{
	public string Command { get; set; }
}

public class UpdateUserSettingsCommandAction
{
	public string Command { get; set; }
}

public class UpdateSettingsButtonCommandAction
{
	public string Command { get; set; }
}

public class UpdateUserAction
{
	public string UserName { get; set; }
	public string IconPath { get; set; }
}
