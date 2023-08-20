using System.Text.Json;
using Fluxor;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.Configuration;

public class Configuration
{
	public List<string> Launchers { get; set; } = new();
}

public class ConfigurationService
{
	private readonly FreeDesktopService _freeDesktopService;
	private readonly IDispatcher _dispatcher;

	public ConfigurationService(FreeDesktopService freeDesktopService, IDispatcher dispatcher)
	{
		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;
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

		var config = JsonSerializer.Deserialize<Configuration>(
			File.ReadAllText(configFile),
			new JsonSerializerOptions(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true });

		foreach (var applicationName in config.Launchers)
		{
			var desktopFile = _freeDesktopService.FindAppDesktopFile(applicationName);
			_dispatcher.Dispatch(new AddDesktopFileAction() { DesktopFile = desktopFile });
		}
	}
}
