using Tmds.DBus;

namespace GtkNetPanel.Tray;

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
