using System.Collections.Immutable;
using Fluxor;
using Gdk;
using Glimpse.Services.Configuration;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public record Entities
{
	public DataTable<string, DesktopFile> DesktopFiles { get; init; } = new();
	public DataTable<string, Pixbuf> NamedIcons { get; init; } = new();
	public DataTable<ulong, WindowProperties> Windows { get; init; } = new();
	public DataTable<ulong, BitmapImage> Screenshots { get; init; } = new();
	public virtual bool Equals(Entities other) => ReferenceEquals(this, other);
}

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

[FeatureState]
public record RootState
{
	public ConfigurationFile Configuration { get; set; } = new();
	public AccountState AccountState { get; init; } = new();
	public Entities Entities { get; set; } = new();
	public SlotReferences TaskbarSlots = new();
	public StartMenuState StartMenuState = new();
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
