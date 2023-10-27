using System.Collections.Immutable;
using Fluxor;
using Gdk;
using Glimpse.Services.Configuration;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

[FeatureState]
public record RootState
{
	public TaskbarState TaskbarState { get; init; } = new();
	public ImmutableList<DesktopFile> DesktopFiles = ImmutableList<DesktopFile>.Empty;
	public UserState UserState { get; init; } = new();
	public string VolumeCommand { get; init; }
	public ImmutableDictionary<IWindowRef, WindowProperties> Windows { get; set; } = ImmutableDictionary<IWindowRef, WindowProperties>.Empty;
	public ImmutableDictionary<IWindowRef, BitmapImage> Screenshots { get; set; } = ImmutableDictionary<IWindowRef, BitmapImage>.Empty;
	public ImmutableList<TaskGroup> Groups { get; set; } = ImmutableList<TaskGroup>.Empty;
	public List<StartMenuLaunchIconContextMenuItem> StartMenuLaunchIconContextMenu { get; set; } = new();
	public ImmutableDictionary<string, Pixbuf> NamedIcons { get; init; } = ImmutableDictionary<string, Pixbuf>.Empty;

	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
}

public record TaskGroup
{
	public string Id { get; init; }
	public DesktopFile DesktopFile { get; init; }
	public ImmutableList<WindowProperties> Windows { get; init; } = ImmutableList<WindowProperties>.Empty;

	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
}

public record WindowProperties
{
	public IWindowRef WindowRef { get; set; }
	public string Title { get; init; }
	public string IconName { get; init; }
	public List<BitmapImage> Icons { get; init; }
	public string ClassHintName { get; init; }
	public string ClassHintClass { get; set; }
	public bool DemandsAttention { get; set; }
	public AllowedWindowActions[] AllowActions { get; set; }
}

public record UserState
{
	public string UserName { get; init; }
	public string IconPath { get; init; }
	public virtual bool Equals(UserState other) => ReferenceEquals(this, other);
}

public record TaskbarState
{
	public ImmutableList<DesktopFile> PinnedDesktopFiles { get; init; } = ImmutableList<DesktopFile>.Empty;
	public string TaskManagerCommand { get; init; }

	public virtual bool Equals(TaskbarState other) => ReferenceEquals(this, other);
}

public class BitmapImage
{
	public int Width { get; init; }
	public int Height { get; init; }
	public byte[] Data { get; init; }
	public int Depth { get; init; }

	public static BitmapImage Empty = new() { Data = Array.Empty<byte>(), Depth = 32, Height = 1, Width = 1 };
}
