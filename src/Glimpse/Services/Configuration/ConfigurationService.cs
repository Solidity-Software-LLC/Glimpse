using System.Reactive.Linq;
using System.Text.Json;
using Fluxor;
using Fluxor.Selectors;
using Glimpse.Components.StartMenu;
using Glimpse.Extensions.Fluxor;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;

namespace Glimpse.Services.Configuration;

public static class ConfigurationSelectors
{
	public static ISelector<ConfigurationFile> Config => SelectorFactory.CreateSelector(
		RootStateSelectors.PinnedTaskbarApps,
		StartMenuSelectors.PinnedStartMenuApps,
		StartMenuSelectors.PowerButtonCommand,
		(pinnedTaskbarApps, pinnedStartMenuApps, powerButtonCommand) =>
		{
			var newConfig = new ConfigurationFile { PowerButtonCommand = powerButtonCommand };
			newConfig.Taskbar.PinnedLaunchers.AddRange(pinnedTaskbarApps.Select(d => d.IniFile.FilePath));
			newConfig.StartMenu.PinnedLaunchers.AddRange(pinnedStartMenuApps.Select(d => d.IniFile.FilePath));
			return newConfig;
		});
}

public class ConfigurationService
{
	private readonly FreeDesktopService _freeDesktopService;
	private readonly IDispatcher _dispatcher;
	private readonly IStore _store;

	public ConfigurationService(FreeDesktopService freeDesktopService, IDispatcher dispatcher, IStore store)
	{
		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;
		_store = store;
	}

	public void Initialize()
	{
		var dataDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "glimpse");
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
		_dispatcher.Dispatch(new UpdateTaskManagerCommandAction() { Command = config.TaskManagerCommand });
		_dispatcher.Dispatch(new UpdateStartMenuLaunchIconContextMenuAction() { MenuItems = config.StartMenuLaunchIconContextMenu });

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

		_store.SubscribeSelector(ConfigurationSelectors.Config).ToObservable().Skip(1).Subscribe(f =>
		{
			Console.WriteLine("Writing");
			File.WriteAllText(configFile, JsonSerializer.Serialize(f, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
		});
	}
}
