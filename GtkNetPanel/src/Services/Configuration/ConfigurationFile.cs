namespace GtkNetPanel.Services.Configuration;

public record ConfigurationFile
{
	public TaskbarConfiguration Taskbar { get; set; } = new();
	public StartMenuConfiguration StartMenu { get; set; } = new();
	public string PowerButtonCommand { get; set; } = "xfce4-session-logout";
}

public class StartMenuConfiguration
{
	public List<string> PinnedLaunchers { get; set; } = new();
}

public class TaskbarConfiguration
{
	public List<string> PinnedLaunchers { get; set; } = new();
}
