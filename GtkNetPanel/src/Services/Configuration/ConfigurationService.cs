using System.Reactive.Linq;
using System.Text.Json;
using Fluxor;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.Configuration;

public class Configuration
{
	public ApplicationBarConfiguration ApplicationBar { get; set; } = new();
	public StartMenuConfiguration StartMenu { get; set; } = new();
}

public class StartMenuConfiguration
{
	public List<string> PinnedLaunchers { get; set; } = new();
}

public class ApplicationBarConfiguration
{
	public List<string> PinnedLaunchers { get; set; } = new();
}

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
			File.WriteAllText(configFile, JsonSerializer.Serialize(new Configuration()));
		}

		// Add file watcher
		var config = JsonSerializer.Deserialize<Configuration>(
			File.ReadAllText(configFile),
			new JsonSerializerOptions(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true });

		foreach (var applicationName in config.ApplicationBar.PinnedLaunchers)
		{
			var desktopFile = _freeDesktopService.FindAppDesktopFile(applicationName);
			_dispatcher.Dispatch(new AddAppBarPinnedDesktopFileAction() { DesktopFile = desktopFile });
		}

		foreach (var applicationName in config.StartMenu.PinnedLaunchers)
		{
			var desktopFile = _freeDesktopService.FindAppDesktopFile(applicationName);
			_dispatcher.Dispatch(new AddStartMenuPinnedDesktopFileAction() { DesktopFile = desktopFile });
		}

		_rootStateSelectors.PinnedAppBar
			.CombineLatest(_rootStateSelectors.PinnedStartMenu)
			.Skip(1)
			.Subscribe(t =>
			{
				Console.WriteLine("Writing");
				var newConfig = new Configuration();
				newConfig.ApplicationBar.PinnedLaunchers.AddRange(t.First.Select(d => d.Name));
				newConfig.StartMenu.PinnedLaunchers.AddRange(t.Second.Select(d => d.Name));
				File.WriteAllText(configFile, JsonSerializer.Serialize(newConfig, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
			});
	}
}
