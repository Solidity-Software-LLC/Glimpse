namespace Glimpse.Services.Configuration;

public record ConfigurationFile
{
	public TaskbarConfiguration Taskbar { get; set; } = new();
	public StartMenuConfiguration StartMenu { get; set; } = new();
	public string PowerButtonCommand { get; set; } = "xfce4-session-logout";
	public string SettingsButtonCommand { get; set; } = "xfce4-settings-manager";
	public string UserSettingsCommand { get; set; } = "mugshot";
	public string VolumeCommand { get; set; } = "pavucontrol";
	public string TaskManagerCommand { get; set; } = "xfce4-taskmanager";
}

public class StartMenuConfiguration
{
	public List<string> PinnedLaunchers { get; set; } = new();
}

public class TaskbarConfiguration
{
	public List<string> PinnedLaunchers { get; set; } = new();
}
