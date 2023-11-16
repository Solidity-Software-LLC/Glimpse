using System.Text.Json;
using System.Text.Json.Serialization;

namespace Glimpse.Services.Configuration;

[JsonSerializable(typeof(ConfigurationFile))]
internal partial class ConfigurationSerializationContext : JsonSerializerContext
{
	public static ConfigurationSerializationContext Instance { get; } = new (new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
}
