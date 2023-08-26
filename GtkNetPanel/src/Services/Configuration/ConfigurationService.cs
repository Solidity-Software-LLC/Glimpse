using System.Reactive.Linq;
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

		foreach (var applicationName in config.Launchers)
		{
			var desktopFile = _freeDesktopService.FindAppDesktopFile(applicationName);
			_dispatcher.Dispatch(new AddPinnedDesktopFileAction() { DesktopFile = desktopFile });
		}

		_rootState
			.ToObservable()
			.Select(s => s.Groups.Where(g => g.IsPinned))
			.Select(s => s.Select(g => g.ApplicationName))
			.DistinctUntilChanged(new EnumerableEqualityComparer<string>())
			.Skip(1)
			.Subscribe(pinnedGroups =>
			{
				Console.WriteLine("Writing");
				var newConfig = new Configuration() { Launchers = pinnedGroups.ToList() };
				File.WriteAllText(configFile, JsonSerializer.Serialize(newConfig, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
			});
	}
}

public class EnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
{
	public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
	{
		if (x == null && y == null) return true;
		if (x == null || y == null) return false;
		if (ReferenceEquals(x, y)) return true;
		return x.SequenceEqual(y);
	}

	public int GetHashCode(IEnumerable<T> obj) => obj.GetHashCode();
}
