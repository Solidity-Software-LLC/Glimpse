using System.Collections.Immutable;
using Gdk;
using Glimpse.Services.Configuration;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public class UpdateGroupOrderingAction
{
	public string GroupId { get; set; }
	public int NewIndex { get; set; }
}

public class UpdateWindowAction
{
	public WindowProperties WindowProperties { get; set; }
}

public class RemoveWindowAction
{
	public WindowProperties WindowProperties { get; set; }
}

public class AddTaskbarPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class ToggleTaskbarPinningAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class UpdateDesktopFilesAction
{
	public ImmutableList<DesktopFile> DesktopFiles { get; init; }
}

public class UpdateVolumeCommandAction
{
	public string Command { get; init; }
}

public class UpdateTaskManagerCommandAction
{
	public string Command { get; set; }
}

public class UpdateUserAction
{
	public string UserName { get; init; }
	public string IconPath { get; init; }
}

public class TakeScreenshotAction
{
	public IEnumerable<IWindowRef> Windows { get; set; }
}

public class UpdateScreenshotsAction
{
	public IEnumerable<(IWindowRef Window, BitmapImage Screenshot)> Screenshots { get; set; }
}

public class UpdateStartMenuLaunchIconContextMenuAction
{
	public List<StartMenuLaunchIconContextMenuItem> MenuItems { get; set; }
}

public class AddOrUpdateNamedIcons
{
	public Dictionary<string, Pixbuf> Icons { get; set; }
}
