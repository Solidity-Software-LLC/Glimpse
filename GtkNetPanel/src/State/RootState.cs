using System.Collections.Immutable;
using Fluxor;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.State;

[FeatureState]
public record RootState
{
	public TaskbarState TaskbarState { get; set; } = new();
	public GenericWindowRef FocusedWindow = new();
	public ImmutableList<DesktopFile> DesktopFiles = ImmutableList<DesktopFile>.Empty;
	public StartMenuState StartMenuState { get; set; } = new();

	public virtual bool Equals(RootState other) => ReferenceEquals(this, other);
}

public record TaskbarState
{
	public ImmutableList<DesktopFile> PinnedDesktopFiles { get; set; } = ImmutableList<DesktopFile>.Empty;
	public ImmutableList<TaskState> Tasks { get; set; } = ImmutableList<TaskState>.Empty;

	public virtual bool Equals(TaskbarState other) => ReferenceEquals(this, other);
}

public record StartMenuState
{
	public ImmutableList<DesktopFile> PinnedDesktopFiles = ImmutableList<DesktopFile>.Empty;
	public string SearchText { get; set; }
	public string PowerButtonCommand { get; set; } = "xfce4-session-logout";
	public string SettingsButtonCommand { get; set; } = "xfce4-settings-manager";
	public string UserSettingsCommand { get; set; } = "mugshot";

	public virtual bool Equals(StartMenuState other) => ReferenceEquals(this, other);
}

public class TaskState
{
	public string Title { get; set; }
	public List<string> State { get; set; }
	public List<BitmapImage> Icons { get; set; }
	public GenericWindowRef WindowRef { get; set; }
	public string ApplicationName { get; set; }
	public DesktopFile DesktopFile { get; set; }
	public AllowedWindowActions[] AllowedActions { get; set; }
	public BitmapImage Screenshot { get; set; }
}

public class BitmapImage
{
	public int Width { get; set; }
	public int Height { get; set; }
	public byte[] Data { get; set; }
}
