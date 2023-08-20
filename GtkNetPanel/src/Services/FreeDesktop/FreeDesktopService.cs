using System.Diagnostics;
using SearchOption = System.IO.SearchOption;

namespace GtkNetPanel.Services.FreeDesktop;

public class FreeDesktopService
{
	private List<DesktopFile> _desktopFiles;

	public void Init()
	{
		var environmentVariables = Environment.GetEnvironmentVariables();

		if (!environmentVariables.Contains("XDG_DATA_DIRS"))
		{
			throw new Exception("XDG_DATA_DIRS environment variables not found");
		}

		var dataDirectories = environmentVariables["XDG_DATA_DIRS"].ToString().Split(":", StringSplitOptions.RemoveEmptyEntries).ToList();
		dataDirectories.Add(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share"));

		_desktopFiles = dataDirectories
			.Distinct()
			.Select(d => Path.Join(d, "applications"))
			.Where(Directory.Exists)
			.SelectMany(d => Directory.EnumerateFiles(d, "*.desktop", SearchOption.AllDirectories))
			.Select(d => DesktopFile.From(ReadIniFile(d)))
			.Where(t => t != null)
			.ToList();
	}

	public DesktopFile FindAppDesktopFile(string applicationName)
	{
		var lowerCaseAppName = applicationName.ToLower();
		var match = _desktopFiles.Where(f => f.Exec.ToLower().Contains(lowerCaseAppName)).ToList().FirstOrDefault();

		if (match == null)
		{
			match = _desktopFiles.Where(f => f.StartupWmClass.ToLower().Contains(lowerCaseAppName)).ToList().FirstOrDefault();
		}

		return match;
	}

	private IniConfiguration ReadIniFile(string filePath)
	{
		try
		{
			return IniConfiguration.Read(File.OpenRead(filePath));
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to read desktop file: " + filePath + Environment.NewLine + e.Message);
		}

		return null;
	}

	public void Run(DesktopFileAction action)
	{
		var parts = action.Exec.Split(" ");
		var executable = parts.FirstOrDefault();
		if (string.IsNullOrEmpty(executable)) return;
		var startInfo = new ProcessStartInfo(executable, string.Join(" ", parts[1..]));
		startInfo.WorkingDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		Process.Start(startInfo);
	}
}
