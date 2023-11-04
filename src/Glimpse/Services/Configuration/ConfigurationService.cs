using System.Reactive.Linq;
using System.Text.Json;
using Fluxor;
using Fluxor.Selectors;
using Glimpse.Extensions.Fluxor;
using Glimpse.State;

namespace Glimpse.Services.Configuration;

public class ConfigurationService
{
	private readonly IDispatcher _dispatcher;
	private readonly IStore _store;

	public ConfigurationService(IDispatcher dispatcher, IStore store)
	{
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

		_dispatcher.Dispatch(new UpdateConfigurationAction() { ConfigurationFile = config });

		_store.SubscribeSelector(RootStateSelectors.Configuration).ToObservable().Skip(1).Subscribe(f =>
		{
			Console.WriteLine("Writing");
			File.WriteAllText(configFile, JsonSerializer.Serialize(f, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
		});
	}
}
