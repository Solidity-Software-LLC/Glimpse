using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Linq;
using Fluxor;
using Glimpse.Extensions.IO;
using Glimpse.Interop;
using Glimpse.Services.DBus;
using Glimpse.Services.DBus.Interfaces;
using Glimpse.State;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace Glimpse.Services.FreeDesktop;

public class FreeDesktopService
{
	private readonly IDispatcher _dispatcher;
	private readonly OrgFreedesktopAccounts _freedesktopAccounts;
	private ImmutableList<DesktopFile> _desktopFiles;
	private IObservable<object> _desktopFileChanged = Observable.Empty<object>();

	public FreeDesktopService(IDispatcher dispatcher, OrgFreedesktopAccounts freedesktopAccounts)
	{
		_dispatcher = dispatcher;
		_freedesktopAccounts = freedesktopAccounts;
	}

	public async Task Init(Connections connections)
	{
		var environmentVariables = Environment.GetEnvironmentVariables();

		if (!environmentVariables.Contains("XDG_DATA_DIRS"))
		{
			throw new Exception("XDG_DATA_DIRS environment variables not found");
		}

		var dataDirectories = environmentVariables["XDG_DATA_DIRS"].ToString().Split(":", StringSplitOptions.RemoveEmptyEntries).ToList();
		dataDirectories.Add(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share"));
		dataDirectories = dataDirectories.Distinct().Select(d => Path.Join(d, "applications")).Where(Directory.Exists).ToList();

		foreach (var d in dataDirectories)
		{
			var watcher = new FileSystemWatcher();
			watcher.Filter = "*.desktop";
			watcher.Path = d;
			watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
			watcher.EnableRaisingEvents = true;

			_desktopFileChanged = _desktopFileChanged
				.Merge(Observable.FromEventPattern(watcher, nameof(watcher.Changed)))
				.Merge(Observable.FromEventPattern(watcher, nameof(watcher.Deleted)))
				.Merge(Observable.FromEventPattern(watcher, nameof(watcher.Created)))
				.Merge(Observable.FromEventPattern(watcher, nameof(watcher.Renamed)));
		}

		_desktopFileChanged.Throttle(TimeSpan.FromSeconds(1)).Subscribe(_ =>
		{
			LoadDesktopFiles(dataDirectories);
		});

		LoadDesktopFiles(dataDirectories);

		var userObjectPath = await _freedesktopAccounts.FindUserByNameAsync(Environment.UserName);
		var userService = new OrgFreedesktopAccountsUser(connections.System, "org.freedesktop.Accounts", userObjectPath);

		Observable
			.Return(await userService.GetAllPropertiesAsync())
			.Concat(userService.PropertiesChanged)
			.Subscribe(p =>
			{
				_dispatcher.Dispatch(new UpdateUserAction() { UserName = p.UserName, IconPath = p.IconFile });
			});
	}

	private void LoadDesktopFiles(IEnumerable<string> dataDirectories)
	{
		_desktopFiles = dataDirectories
			.SelectMany(d => Directory.EnumerateFiles(d, "*.desktop", SearchOption.AllDirectories))
			.Select(d => DesktopFile.From(ReadIniFile(d)))
			.Where(t => t != null)
			.ToImmutableList();

		_dispatcher.Dispatch(new UpdateDesktopFilesAction() { DesktopFiles = _desktopFiles });
	}

	public DesktopFile FindAppDesktopFileByPath(string filePath)
	{
		return _desktopFiles.FirstOrDefault(f => f.IniFile.FilePath.Equals(filePath, StringComparison.InvariantCultureIgnoreCase));
	}

	private IniFile ReadIniFile(string filePath)
	{
		try
		{
			var iniFile = File.OpenRead(filePath);
			var iniConfig = IniFile.Read(iniFile);
			iniConfig.FilePath = filePath;
			return iniConfig;
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to read desktop file: " + filePath + Environment.NewLine + e.Message);
		}

		return null;
	}

	public void Run(DesktopFile desktopFile)
	{
		var startInfo = new ProcessStartInfo("setsid", "xdg-open " + desktopFile.IniFile.FilePath);
		startInfo.WorkingDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		Process.Start(startInfo);
	}

	public void Run(DesktopFileAction action)
	{
		var gDesktopFile = LibGdk3Interop.g_desktop_app_info_new_from_filename(action.DesktopFilePath);
		LibGdk3Interop.g_desktop_app_info_launch_action(gDesktopFile, action.Id, IntPtr.Zero);
	}

	public void Run(string command)
	{
		var startInfo = new ProcessStartInfo("setsid", command);
		startInfo.WorkingDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		Process.Start(startInfo);
	}
}
