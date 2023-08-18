namespace GtkNetPanel.Services.FreeDesktop;

public class DesktopFile
{
	public string Name { get; set; }
	public string IconName { get; set; }
	public string StartupWmClass { get; set; }
	public string Exec { get; set; }
	public List<DesktopFileAction> Actions { get; set; }

	public static DesktopFile From(IniConfiguration configuration)
	{
		var desktopEntry = configuration.Sections.FirstOrDefault(s => s.Header.Equals("Desktop Entry", StringComparison.InvariantCultureIgnoreCase));

		if (desktopEntry == null)
		{
			throw new Exception("No desktop entry section in ini file");
		}

		desktopEntry.NameValuePairs.TryGetValue("Name", out var name);
		desktopEntry.NameValuePairs.TryGetValue("Icon", out var iconName);
		desktopEntry.NameValuePairs.TryGetValue("Exec", out var exec);
		desktopEntry.NameValuePairs.TryGetValue("StartupWMClass", out var startupWmClass);

		return new DesktopFile()
		{
			Name = name ?? "",
			IconName = iconName ?? "",
			Exec = exec ?? "",
			StartupWmClass = startupWmClass ?? "",
			Actions = ParseActions(configuration) ?? new List<DesktopFileAction>()
		};
	}

	private static List<DesktopFileAction> ParseActions(IniConfiguration configuration)
	{
		var desktopEntry = configuration.Sections.FirstOrDefault(s => s.Header.Equals("Desktop Entry", StringComparison.InvariantCultureIgnoreCase));

		if (desktopEntry == null)
		{
			return null;
		}

		if (!desktopEntry.NameValuePairs.TryGetValue("Actions", out var actions))
		{
			return null;
		}

		var results = new List<DesktopFileAction>();

		foreach (var actionName in actions.Split(";", StringSplitOptions.RemoveEmptyEntries))
		{
			var actionSection = configuration.Sections.FirstOrDefault(s => s.Header.Contains(actionName));

			if (actionSection == null)
			{
				continue;
			}

			results.Add(DesktopFileAction.Parse(actionSection));
		}

		return results;
	}
}
