using System.Collections.Immutable;
using Gdk;
using Glimpse.Common.Images;

namespace Glimpse.UI.State;

public class SlotReferences : IEquatable<SlotReferences>
{
	public ImmutableList<SlotRef> Refs = ImmutableList<SlotRef>.Empty;

	public bool Equals(SlotReferences other)
	{
		return other.Refs.SequenceEqual(Refs);
	}
}

public record SlotRef
{
	public string PinnedDesktopFileId { get; init; } = "";
	public string ClassHintName { get; init; } = "";
	public string DiscoveredDesktopFileId { get; init; } = "";
}

public record ImageViewModel
{
	public string IconNameOrPath { get; set; } = "";
	public IGlimpseImage Image { get; set; }
}

public record UIState
{
	public SlotReferences TaskbarSlots = new();

	public virtual bool Equals(UIState other) => ReferenceEquals(this, other);
}

public record StartMenuState
{
	public string SearchText { get; init; } = "";

	public ImmutableDictionary<StartMenuChips, StartMenuAppFilteringChip> Chips { get; init; } =
		ImmutableDictionary<StartMenuChips, StartMenuAppFilteringChip>.Empty
			.Add(StartMenuChips.Pinned, new() { IsSelected = true, IsVisible = true })
			.Add(StartMenuChips.AllApps, new() { IsSelected = false, IsVisible = true })
			.Add(StartMenuChips.SearchResults, new() { IsSelected = false, IsVisible = false });

	public virtual bool Equals(StartMenuState other) => ReferenceEquals(this, other);
}

public enum StartMenuChips
{
	Pinned,
	AllApps,
	SearchResults
}

public record StartMenuAppFilteringChip
{
	public bool IsVisible { get; set; }
	public bool IsSelected { get; set; }
}
