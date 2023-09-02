using System.Reactive.Linq;
using System.Text.Json;
using Fluxor;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.Configuration;

public class ConfigurationService
{
	private readonly FreeDesktopService _freeDesktopService;
	private readonly IDispatcher _dispatcher;
	private readonly RootStateSelectors _rootStateSelectors;

	public ConfigurationService(FreeDesktopService freeDesktopService, IDispatcher dispatcher, RootStateSelectors rootStateSelectors)
	{
		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;
		_rootStateSelectors = rootStateSelectors;
	}

	public void Initialize()
	{
		var dataDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SharpPanel");
		var configFile = Path.Join(dataDirectory, "config.json");

		if (!Directory.Exists(dataDirectory))
		{
			Directory.CreateDirectory(dataDirectory);
		}

		if (!File.Exists(configFile))
		{
			File.WriteAllText(configFile, JsonSerializer.Serialize(new ConfigurationFile()));
		}

		// Add file watcher
		var config = JsonSerializer.Deserialize<ConfigurationFile>(
			File.ReadAllText(configFile),
			new JsonSerializerOptions(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true });

		_dispatcher.Dispatch(new UpdatePowerButtonCommandAction() { Command = config.PowerButtonCommand });
		_dispatcher.Dispatch(new UpdateSettingsButtonCommandAction() { Command = config.SettingsButtonCommand });
		_dispatcher.Dispatch(new UpdateUserSettingsCommandAction() { Command = config.UserSettingsCommand });
		_dispatcher.Dispatch(new UpdateVolumeCommandAction() { Command = config.VolumeCommand });

		foreach (var filePath in config.Taskbar.PinnedLaunchers)
		{
			var desktopFile = _freeDesktopService.FindAppDesktopFileByPath(filePath);
			_dispatcher.Dispatch(new AddTaskbarPinnedDesktopFileAction() { DesktopFile = desktopFile });
		}

		foreach (var filePath in config.StartMenu.PinnedLaunchers)
		{
			var desktopFile = _freeDesktopService.FindAppDesktopFileByPath(filePath);
			_dispatcher.Dispatch(new AddStartMenuPinnedDesktopFileAction() { DesktopFile = desktopFile });
		}

		_rootStateSelectors.PinnedTaskbarApps
			.CombineLatest(_rootStateSelectors.PinnedStartMenuApps, _rootStateSelectors.PowerButtonCommand)
			.Skip(1)
			.Subscribe(t =>
			{
				Console.WriteLine("Writing");
				var newConfig = new ConfigurationFile { PowerButtonCommand = t.Third };
				newConfig.Taskbar.PinnedLaunchers.AddRange(t.First.Select(d => d.IniFile.FilePath));
				newConfig.StartMenu.PinnedLaunchers.AddRange(t.Second.Select(d => d.IniFile.FilePath));
				File.WriteAllText(configFile, JsonSerializer.Serialize(newConfig, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
			});
	}
}
