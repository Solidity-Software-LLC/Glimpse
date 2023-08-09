using GtkNetPanel.DBus.Menu;
using GtkNetPanel.DBus.StatusNotifierItem;
using Tmds.DBus;
using Task = System.Threading.Tasks.Task;

namespace GtkNetPanel.DBus;

public class DBus
{
	public static async Task ActivateSystemTrayItemAsync(StatusNotifierItem.DbusStatusNotifierItem dbusStatusNotifierItem, int x, int y)
	{
		var itemProxy = Connection.Session.CreateProxy<IStatusNotifierItem>(dbusStatusNotifierItem.Object.ServiceName, dbusStatusNotifierItem.Object.ObjectPath);
		await itemProxy.ActivateAsync(x, y);
	}

	public static async Task ContextMenuAsync(StatusNotifierItem.DbusStatusNotifierItem dbusStatusNotifierItem, int x, int y)
	{
		var itemProxy = Connection.Session.CreateProxy<IStatusNotifierItem>(dbusStatusNotifierItem.Object.ServiceName, dbusStatusNotifierItem.Object.ObjectPath);
		await itemProxy.ContextMenuAsync(x, y);
	}

	public static async Task SecondaryActivateAsync(StatusNotifierItem.DbusStatusNotifierItem dbusStatusNotifierItem, int x, int y)
	{
		var itemProxy = Connection.Session.CreateProxy<IStatusNotifierItem>(dbusStatusNotifierItem.Object.ServiceName, dbusStatusNotifierItem.Object.ObjectPath);
		await itemProxy.SecondaryActivateAsync(x, y);
	}

	public static async Task<DbusMenuItem> GetMenuItems(StatusNotifierItem.DbusStatusNotifierItem dbusStatusNotifierItem)
	{
		var menuProxy = Connection.Session.CreateProxy<IDbusmenu>(dbusStatusNotifierItem.Menu.ServiceName, dbusStatusNotifierItem.Menu.ObjectPath);
		var dbusResult = await menuProxy.GetLayoutAsync(0, -1, Array.Empty<string>());
		return DbusMenuItem.From(dbusResult.layout);
	}

	public static async Task ClickedItem(StatusNotifierItem.DbusStatusNotifierItem dbusStatusNotifierItem, int id)
	{
		var menuProxy = Connection.Session.CreateProxy<IDbusmenu>(dbusStatusNotifierItem.Menu.ServiceName, dbusStatusNotifierItem.Menu.ObjectPath);
		await menuProxy.EventAsync(id, "clicked", new byte[1], 0);
	}
}
