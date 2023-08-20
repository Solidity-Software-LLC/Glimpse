namespace GtkNetPanel.Services.FreeDesktop;

public class DesktopFile
{
	public string Name { get; set; }
	public string IconName { get; set; }
	public string StartupWmClass { get; set; }
	public DesktopFileExec Exec { get; set; }
	public List<DesktopFileAction> Actions { get; set; }
	public List<string> Categories { get; set; }
	public IniConfiguration IniConfiguration { get; set; }

	public static DesktopFile From(IniConfiguration configuration)
	{
		var desktopEntry = configuration.Sections.FirstOrDefault(s => s.Header.Equals("Desktop Entry", StringComparison.InvariantCultureIgnoreCase));

		if (desktopEntry == null)
		{
			throw new Exception("No desktop entry section in ini file");
		}

		desktopEntry.NameValuePairs.TryGetValue("Name", out var name);
		desktopEntry.NameValuePairs.TryGetValue("Icon", out var icon);
		desktopEntry.NameValuePairs.TryGetValue("Exec", out var exec);
		desktopEntry.NameValuePairs.TryGetValue("Categories", out var categories);
		desktopEntry.NameValuePairs.TryGetValue("StartupWMClass", out var startupWmClass);

		var desktopFile = new DesktopFile()
		{
			IniConfiguration = configuration,
			Name = name ?? "",
			IconName = icon ?? "",
			Exec = ParseExec(exec) ?? new DesktopFileExec(),
			StartupWmClass = startupWmClass ?? "",
			Actions = ParseActions(configuration) ?? new List<DesktopFileAction>(),
			Categories = ParseCategories(categories) ?? new List<string>()
		};

		return desktopFile;
	}

	//private static string[] s_allExecPlaceholders = new[] { "%f", "%F", "%u", "%U", "%d", "%D", "%n", "%N", "%i", "%c", "%k", "%v", "%m" };
	private static readonly string[] s_execPlaceholders = new[] { "%f", "%F", "%u", "%U" };

	private static DesktopFileExec ParseExec(string exec)
	{
		if (string.IsNullOrEmpty(exec)) return null;

		foreach (var ph in s_execPlaceholders) exec = exec.Replace(ph, "");

		var parts = exec.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var result = new DesktopFileExec();
		result.FullExec = exec;
		result.Executable = parts[0];
		if (parts.Length > 1) result.Arguments = string.Join(" ", parts[1..]);
		return result;
	}

	private static List<string> ParseCategories(string categories)
	{
		if (string.IsNullOrEmpty(categories)) return null;
		return categories.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList();
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
