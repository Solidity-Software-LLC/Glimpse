using System.Collections.Immutable;
using Gdk;
using Glimpse.Services.Configuration;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public class UpdateTaskbarSlotOrderingSingleAction
{
	public SlotRef SlotRef { get; set; }
	public int NewIndex { get; set; }
}

public class UpdateTaskbarSlotOrderingBulkAction
{
	public ImmutableList<SlotRef> Slots { get; set; }
}

public record AddWindowAction(WindowProperties WindowProperties);

public class UpdateWindowAction
{
	public WindowProperties WindowProperties { get; set; }
}

public class RemoveWindowAction
{
	public WindowProperties WindowProperties { get; set; }
}

public record ToggleTaskbarPinningAction(string DesktopFileId);

public class UpdateDesktopFilesAction
{
	public ImmutableList<DesktopFile> DesktopFiles { get; init; }
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
	public Dictionary<ulong, BitmapImage> Screenshots { get; set; }
}

public class AddOrUpdateNamedIconsAction
{
	public Dictionary<string, Pixbuf> Icons { get; set; }
}

public class UpdateConfigurationAction
{
	public ConfigurationFile ConfigurationFile { get; set; }
}

public record ToggleStartMenuPinningAction(string DesktopFileId);
public record UpdateStartMenuSearchTextAction(string SearchText);
public record UpdateStartMenuPinnedAppOrderingAction(string DesktopFileKey, int NewIndex);
public record UpdateAppFilteringChip(StartMenuChips Chip);
