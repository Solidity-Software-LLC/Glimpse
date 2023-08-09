using Tmds.DBus;

namespace GtkNetPanel.DBus.StatusNotifierWatcher;

[DBusInterface(DbusInterfaceName)]
internal interface IStatusNotifierWatcher : IDBusObject
{
	public const string DbusInterfaceName = "org.kde.StatusNotifierWatcher";

	Task RegisterStatusNotifierItemAsync(string Service);
	Task RegisterStatusNotifierHostAsync(string Service);
	Task<IDisposable> WatchStatusNotifierItemRegisteredAsync(Action<string> handler, Action<Exception> onError = null);
	Task<IDisposable> WatchStatusNotifierItemUnregisteredAsync(Action<string> handler, Action<Exception> onError = null);
	Task<IDisposable> WatchStatusNotifierHostRegisteredAsync(Action handler, Action<Exception> onError = null);
	Task<T> GetAsync<T>(string prop);
	Task<StatusNotifierWatcherProperties> GetAllAsync();
	Task SetAsync(string prop, object val);
	Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}

static class StatusNotifierWatcherExtensions
{
	public static Task<string[]> GetRegisteredStatusNotifierItemsAsync(this IStatusNotifierWatcher o) => o.GetAsync<string[]>("RegisteredStatusNotifierItems");
	public static Task<bool> GetIsStatusNotifierHostRegisteredAsync(this IStatusNotifierWatcher o) => o.GetAsync<bool>("IsStatusNotifierHostRegistered");
	public static Task<int> GetProtocolVersionAsync(this IStatusNotifierWatcher o) => o.GetAsync<int>("ProtocolVersion");
}
