using Tmds.DBus;

namespace GtkNetPanel.DBus.Menu;

[DBusInterface(DbusInterfaceName)]
internal interface IDbusmenu : IDBusObject
{
	public const string DbusInterfaceName = "com.canonical.dbusmenu";

	Task<(uint revision, (int, IDictionary<string, object>, object[]) layout)> GetLayoutAsync(int ParentId, int RecursionDepth, string[] PropertyNames);
	Task<(int, IDictionary<string, object>)[]> GetGroupPropertiesAsync(int[] Ids, string[] PropertyNames);
	Task<object> GetPropertyAsync(int Id, string Name);
	Task EventAsync(int Id, string EventId, object Data, uint Timestamp);
	Task<int[]> EventGroupAsync((int, string, object, uint)[] Events);
	Task<bool> AboutToShowAsync(int Id);
	Task<(int[] updatesNeeded, int[] idErrors)> AboutToShowGroupAsync(int[] Ids);
	Task<IDisposable> WatchItemsPropertiesUpdatedAsync(Action<((int, IDictionary<string, object>)[] updatedProps, (int, string[])[] removedProps)> handler, Action<Exception> onError = null);
	Task<IDisposable> WatchLayoutUpdatedAsync(Action<(uint revision, int parent)> handler, Action<Exception> onError = null);
	Task<IDisposable> WatchItemActivationRequestedAsync(Action<(int id, uint timestamp)> handler, Action<Exception> onError = null);
	Task<T> GetAsync<T>(string prop);
	Task<DbusmenuProperties> GetAllAsync();
	Task SetAsync(string prop, object val);
	Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
