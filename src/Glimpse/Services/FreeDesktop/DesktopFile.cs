using Glimpse.Extensions.IO;

namespace Glimpse.Services.FreeDesktop;

public class DesktopFile
{
	public string Name { get; set; }
	public string IconName { get; set; } = "application-default-icon";
	public string StartupWmClass { get; set; } = "";
	public DesktopFileExec Exec { get; set; } = new();
	public List<DesktopFileAction> Actions { get; set; } = new();
	public List<string> Categories { get; set; } = new();
	public IniFile IniFile { get; set; }

	public static DesktopFile From(IniFile file)
	{
		var desktopEntry = file.Sections.FirstOrDefault(s => s.Header.Equals("Desktop Entry", StringComparison.InvariantCultureIgnoreCase));

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
			IniFile = file,
			Name = name ?? "",
			IconName = icon ?? "",
			Exec = ParseExec(exec) ?? new DesktopFileExec(),
			StartupWmClass = startupWmClass ?? "",
			Actions = ParseActions(file) ?? new List<DesktopFileAction>(),
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

	private static List<DesktopFileAction> ParseActions(IniFile file)
	{
		var desktopEntry = file.Sections.FirstOrDefault(s => s.Header.Equals("Desktop Entry", StringComparison.InvariantCultureIgnoreCase));

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
			var actionSection = file.Sections.FirstOrDefault(s => s.Header.Contains(actionName));

			if (actionSection == null)
			{
				continue;
			}

			results.Add(DesktopFileAction.Parse(actionSection));
		}

		return results;
	}
}

public static class DesktopFileExtensions
{
	public static DesktopFile FindByApplicationName(this IEnumerable<DesktopFile> files, string applicationName)
	{
		var lowerCaseAppName = applicationName.ToLower();

		return files.FirstOrDefault(f => f.StartupWmClass.ToLower() == lowerCaseAppName)
			?? files.FirstOrDefault(f => f.Name.ToLower().Contains(lowerCaseAppName))
			?? files.FirstOrDefault(f => f.StartupWmClass.ToLower().Contains(lowerCaseAppName))
			?? files.FirstOrDefault(f => f.Exec.Executable.ToLower().Contains(lowerCaseAppName))
			?? files.FirstOrDefault(f => f.Exec.Executable.ToLower() == lowerCaseAppName);
	}
}
