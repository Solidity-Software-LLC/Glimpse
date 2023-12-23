using System.Collections.Immutable;
using Glimpse.Images;

namespace Glimpse.UI.State;

public class StartMenuOpenedAction
{

}

public class UpdateTaskbarSlotOrderingBulkAction
{
	public ImmutableList<SlotRef> Slots { get; set; }
}

public record ToggleTaskbarPinningAction(string DesktopFileId);

public class AddOrUpdateNamedIconsAction
{
	public Dictionary<string, IGlimpseImage> Icons { get; set; }
}

public record ToggleStartMenuPinningAction(string DesktopFileId);
public record UpdateStartMenuSearchTextAction(string SearchText);
public record UpdateStartMenuPinnedAppOrderingAction(ImmutableList<string> DesktopFileKeys);
public record UpdateAppFilteringChip(StartMenuChips Chip);
