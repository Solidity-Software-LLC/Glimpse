using System.Reactive.Linq;
using System.Text.Json;
using Glimpse.Extensions.Redux;
using Glimpse.State;

namespace Glimpse.Services.Configuration;

public class ConfigurationService(ReduxStore store)
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
			File.WriteAllText(configFile, JsonSerializer.Serialize(new ConfigurationFile(), typeof(ConfigurationFile), ConfigurationSerializationContext.Instance));
		}

		// Add file watcher
		var config = (ConfigurationFile) JsonSerializer.Deserialize(
			File.ReadAllText(configFile),
			typeof(ConfigurationFile),
			ConfigurationSerializationContext.Instance);

		store.Dispatch(new UpdateConfigurationAction() { ConfigurationFile = config });

		store.Select(RootStateSelectors.Configuration).Skip(1).Subscribe(f =>
		{
			Console.WriteLine("Writing");
			File.WriteAllText(configFile, JsonSerializer.Serialize(f, typeof(ConfigurationFile), ConfigurationSerializationContext.Instance));
		});

		return Task.CompletedTask;
	}
}
