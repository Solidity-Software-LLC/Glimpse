using GtkNetPanel.Services.DBus.Menu;
using GtkNetPanel.Services.DBus.StatusNotifierItem;
using Tmds.DBus;
using Task = System.Threading.Tasks.Task;

namespace GtkNetPanel.Services.DBus;

// ConvertToEffects
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

	public static async Task ClickedItem(StatusNotifierItem.DbusStatusNotifierItem dbusStatusNotifierItem, int id)
	{
		var menuProxy = Connection.Session.CreateProxy<IDbusmenu>(dbusStatusNotifierItem.Menu.ServiceName, dbusStatusNotifierItem.Menu.ObjectPath);
		await menuProxy.EventAsync(id, "clicked", new byte[1], 0);
	}
}
