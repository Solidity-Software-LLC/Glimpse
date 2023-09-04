using System.Collections.Immutable;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public class AddTaskAction
{
	public TaskState Task { get; init; }
}

public class RemoveTaskAction
{
	public string WindowId { get; init; }
}

public class UpdateFocusAction
{
	public GenericWindowRef WindowRef { get; init; }
}

public class AddTaskbarPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class AddStartMenuPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class UpdateStartMenuSearchTextAction
{
	public string SearchText { get; init; }
}

public class ToggleTaskbarPinningAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class ToggleStartMenuPinningAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class UpdateDesktopFilesAction
{
	public ImmutableList<DesktopFile> DesktopFiles { get; init; }
}

public class UpdatePowerButtonCommandAction
{
	public string Command { get; init; }
}

public class UpdateUserSettingsCommandAction
{
	public string Command { get; init; }
}

public class UpdateSettingsButtonCommandAction
{
	public string Command { get; init; }
}

public class UpdateVolumeCommandAction
{
	public string Command { get; init; }
}

public class UpdateUserAction
{
	public string UserName { get; init; }
	public string IconPath { get; init; }
}
