using System.Reactive.Linq;
using System.Text.Json;
using Fluxor;
using Fluxor.Selectors;
using Glimpse.Extensions.Fluxor;
using Glimpse.State;

namespace Glimpse.Services.Configuration;

public class ConfigurationService(IDispatcher dispatcher, IStore store)
{
	public Task InitializeAsync()
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
		var config = (ConfigurationFile) JsonSerializer.Deserialize(
			File.ReadAllText(configFile),
			typeof(ConfigurationFile),
			ConfigurationSerializationContext.Instance);

		dispatcher.Dispatch(new UpdateConfigurationAction() { ConfigurationFile = config });

		store.SubscribeSelector(RootStateSelectors.Configuration).ToObservable().Skip(1).Subscribe(f =>
		{
			Console.WriteLine("Writing");
			File.WriteAllText(configFile, JsonSerializer.Serialize(f, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true }));
		});

		return Task.CompletedTask;
	}
}
