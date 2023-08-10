using Tmds.DBus;

namespace GtkNetPanel.Services.DBus.StatusNotifierItem;

[DBusInterface(DbusInterfaceName)]
public interface IStatusNotifierItem : IDBusObject
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

public static class StatusNotifierItemExtensions
{
	public static async Task<T> TryGetAsync<T>(this IStatusNotifierItem item, string prop)
	{
		try
		{
			return await item.GetAsync<T>(prop);
		}
		catch (Exception e)
		{
			return default;
		}
	}
}
