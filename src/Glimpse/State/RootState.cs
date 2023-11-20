using System.Collections.Immutable;
using Glimpse.Services.DisplayServer;

namespace Glimpse.State;

public record WindowProperties : IKeyed<ulong>
{
	public ulong Id => WindowRef.Id;
	public IWindowRef WindowRef { get; set; }
	public string Title { get; init; }
	public string IconName { get; init; }
	public List<BitmapImage> Icons { get; init; }
	public string ClassHintName { get; init; }
	public string ClassHintClass { get; set; }
	public bool DemandsAttention { get; set; }
	public AllowedWindowActions[] AllowActions { get; set; }
	public uint Pid { get; set; }
	public virtual bool Equals(WindowProperties other) => ReferenceEquals(this, other);
}

public record AccountState
{
	public string UserName { get; init; }
	public string IconPath { get; init; }
	public virtual bool Equals(AccountState other) => ReferenceEquals(this, other);
}

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

public record RootState
{
	public SlotReferences TaskbarSlots = new();
	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
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
