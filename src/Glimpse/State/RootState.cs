using System.Collections.Immutable;
using Fluxor;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

[FeatureState]
public record RootState
{
	public TaskbarState TaskbarState { get; init; } = new();
	public ImmutableList<DesktopFile> DesktopFiles = ImmutableList<DesktopFile>.Empty;
	public StartMenuState StartMenuState { get; init; } = new();
	public UserState UserState { get; init; } = new();
	public string VolumeCommand { get; init; }
	public ImmutableDictionary<IWindowRef, BitmapImage> Screenshots { get; set; } = ImmutableDictionary<IWindowRef, BitmapImage>.Empty;
	public ImmutableList<TaskGroup> Groups { get; set; } = ImmutableList<TaskGroup>.Empty;

	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
}

public record TaskGroup
{
	public string Id { get; init; }
	public DesktopFile DesktopFile { get; init; }
	public ImmutableList<WindowProperties> Windows { get; init; } = ImmutableList<WindowProperties>.Empty;

	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
}

public class WindowProperties
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

	public virtual bool Equals(TaskbarState other) => ReferenceEquals(this, other);
}

public record StartMenuState
{
	public ImmutableList<DesktopFile> PinnedDesktopFiles = ImmutableList<DesktopFile>.Empty;
	public string SearchText { get; init; }
	public string PowerButtonCommand { get; init; } = "xfce4-session-logout";
	public string SettingsButtonCommand { get; init; } = "xfce4-settings-manager";
	public string UserSettingsCommand { get; init; } = "mugshot";

	public virtual bool Equals(StartMenuState other) => ReferenceEquals(this, other);
}

public class TaskState
{
	public string Title { get; init; }
	public bool DemandsAttention { get; init; }
	public List<BitmapImage> Icons { get; init; }
	public IWindowRef WindowRef { get; init; }
	public string ApplicationName { get; init; }
	public DesktopFile DesktopFile { get; init; }
	public AllowedWindowActions[] AllowedActions { get; init; }
	public BitmapImage Screenshot { get; init; }
}

public class BitmapImage
{
	public int Width { get; init; }
	public int Height { get; init; }
	public byte[] Data { get; init; }
	public int Depth { get; init; }
}
