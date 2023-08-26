using System.Reactive.Linq;
using System.Text.Json;
using Fluxor;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.Configuration;

public class Configuration
{
	public ApplicationBarConfiguration ApplicationBar { get; set; } = new();
	public ApplicationMenuConfiguration ApplicationMenu { get; set; } = new();
}

public class ApplicationMenuConfiguration
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
	private readonly IState<RootState> _rootState;

	public ConfigurationService(FreeDesktopService freeDesktopService, IDispatcher dispatcher, IState<RootState> rootState)
	{
		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;
		_rootState = rootState;
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

		foreach (var applicationName in config.ApplicationBar.PinnedLaunchers)
		{
			var desktopFile = _freeDesktopService.FindAppDesktopFile(applicationName);
			_dispatcher.Dispatch(new AddAppBarPinnedDesktopFileAction() { DesktopFile = desktopFile });
		}

		var groupComparer = new FuncEqualityComparer<ApplicationGroupState>((x, y) => x.ApplicationName == y.ApplicationName);

		var pinnedAppBarObs = _rootState
			.ToObservable()
			.Select(s => s.Groups)
			.Select(s => s.Where(g => g.IsPinnedToApplicationBar))
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y, groupComparer));

		var pinnedAppMenuObs = _rootState
			.ToObservable()
			.Select(s => s.Groups)
			.Select(s => s.Where(g => g.IsPinnedToApplicationMenu))
			.DistinctUntilChanged((x, y) => x.SequenceEqual(y, groupComparer));

		pinnedAppBarObs
			.CombineLatest(pinnedAppMenuObs)
			.Skip(1)
			.Subscribe(t =>
			{
				Console.WriteLine("Writing");
				var newConfig = new Configuration();
				newConfig.ApplicationBar.PinnedLaunchers.AddRange(t.First.Select(g => g.ApplicationName));
				newConfig.ApplicationMenu.PinnedLaunchers.AddRange(t.Second.Select(g => g.ApplicationName));
				File.WriteAllText(configFile, JsonSerializer.Serialize(newConfig, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
			});
	}
}
