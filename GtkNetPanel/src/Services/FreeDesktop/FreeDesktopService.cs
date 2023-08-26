using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Subjects;
using Fluxor;
using GtkNetPanel.State;
using SearchOption = System.IO.SearchOption;

namespace GtkNetPanel.Services.FreeDesktop;

public class FreeDesktopService
{
	private readonly IDispatcher _dispatcher;
	private ImmutableList<DesktopFile> _desktopFiles;

	public FreeDesktopService(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

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
			.ToImmutableList();

		_dispatcher.Dispatch(new UpdateDesktopFilesAction() { DesktopFiles = _desktopFiles });
	}

	public DesktopFile FindAppDesktopFile(string applicationName)
	{
		var lowerCaseAppName = applicationName.ToLower();

		return _desktopFiles.FirstOrDefault(f => f.StartupWmClass.ToLower() == lowerCaseAppName)
				?? _desktopFiles.FirstOrDefault(f => f.Name.ToLower().Contains(lowerCaseAppName))
				?? _desktopFiles.FirstOrDefault(f => f.StartupWmClass.ToLower().Contains(lowerCaseAppName))
				?? _desktopFiles.FirstOrDefault(f => f.Exec.Executable.ToLower().Contains(lowerCaseAppName))
				?? _desktopFiles.FirstOrDefault(f => f.Exec.Executable.ToLower() == lowerCaseAppName);
	}

	private IniConfiguration ReadIniFile(string filePath)
	{
		try
		{
			var iniFile = File.OpenRead(filePath);
			var iniConfig = IniConfiguration.Read(iniFile);
			iniConfig.FilePath = filePath;
			return iniConfig;
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to read desktop file: " + filePath + Environment.NewLine + e.Message);
		}

		return null;
	}

	public void Run(string exec)
	{
		var parts = exec.Split(" ");
		var executable = parts.FirstOrDefault();
		if (string.IsNullOrEmpty(executable)) return;
		var startInfo = new ProcessStartInfo(executable, string.Join(" ", parts[1..]));
		startInfo.WorkingDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		Process.Start(startInfo);
	}

	public void Run(DesktopFileAction action)
	{
		Run(action.Exec);
	}
}
