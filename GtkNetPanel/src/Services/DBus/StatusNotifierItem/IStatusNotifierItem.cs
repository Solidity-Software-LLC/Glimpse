using Tmds.DBus;

namespace GtkNetPanel.DBus.StatusNotifierItem;

[DBusInterface(DbusInterfaceName)]
internal interface IStatusNotifierItem : IDBusObject
{
	public const string DbusInterfaceName = "org.kde.StatusNotifierItem";

	Task ContextMenuAsync(int arg0, int arg1);
	Task ActivateAsync(int arg0, int arg1);
	Task SecondaryActivateAsync(int arg0, int arg1);
	Task ScrollAsync(int arg0, string arg1);
	Task<T> GetAsync<T>(string prop);
	Task<IDisposable> WatchNewStatusAsync(Action<string> handler, Action<Exception> onError = null);
	Task<IDisposable> WatchNewToolTipAsync(Action handler, Action<Exception> onError = null);
	Task<IDisposable> WatchNewOverlayIconAsync(Action handler, Action<Exception> onError = null);
	Task<IDisposable> WatchNewAttentionIconAsync(Action handler, Action<Exception> onError = null);
	Task<IDisposable> WatchNewIconAsync(Action handler, Action<Exception> onError = null);
	Task<IDisposable> WatchNewTitleAsync(Action handler, Action<Exception> onError = null);
	Task<StatusNotifierItemProperties> GetAllAsync();
}
