using System.Xml.Linq;
using System.Xml.XPath;
using Tmds.DBus;
using Task = System.Threading.Tasks.Task;

namespace GtkNetPanel.Tray;

public class DBus
{
	public static async Task<List<StatusNotifierItem>> GetTrayItems()
	{
		var results = new List<StatusNotifierItem>();
		var dbus = Connection.Session.CreateProxy<IStatusNotifierWatcher>(IStatusNotifierWatcher.DbusInterfaceName, "/StatusNotifierWatcher");
		var props = await dbus.GetAllAsync();

		foreach (var item in props.RegisteredStatusNotifierItems)
		{
			var serviceName = item.Substring(0, item.IndexOf("/", StringComparison.Ordinal));
			var endpoint = await FindDbusInterface(serviceName, "/", i => i == IStatusNotifierItem.DbusInterfaceName);

			if (endpoint == null)
			{
				continue;
			}

			var itemProxy = Connection.Session.CreateProxy<IStatusNotifierItem>(serviceName, endpoint.ObjectPath);
			var statusItem = new StatusNotifierItem { Properties = await itemProxy.GetAllAsync(), Object = endpoint };
			var menuPath = await TryGetProp<string>(itemProxy, "Menu") ?? (await TryGetProp<ObjectPath>(itemProxy, "Menu")).ToString();

			if (!string.IsNullOrEmpty(menuPath))
			{
				var dbusMenuObject = await FindDbusInterface(serviceName, menuPath, p => p == IDbusmenu.DbusInterfaceName);
				statusItem = statusItem with { Menu = dbusMenuObject };
			}

			results.Add(statusItem);
		}

		return results.DistinctBy(r => r.Properties.Id).ToList();
	}

	private static async Task<DbusObject> FindDbusInterface(string serviceName, string objectPath, Func<string, bool> match)
	{
		var introProxy = Connection.Session.CreateProxy<IIntrospectable>(serviceName, objectPath);
		var rawXml = await introProxy.IntrospectAsync();
		var xml = XDocument.Parse(rawXml);

		foreach (var i in xml.XPathSelectElements("//node/interface"))
		{
			if (!match(i.Attribute("name").Value)) continue;

			return new DbusObject()
			{
				ServiceName = serviceName,
				ObjectPath = objectPath,
				Xml = rawXml,
				Interfaces = xml
					.XPathSelectElements("//interface")
					.Select(x => new DbusInterface()
					{
						Name = x.Attribute("name").Value,
						Methods = x.XPathSelectElements("./method").Select(m => m.Attribute("name").Value).ToArray()
					}).ToList()
			};
		}

		foreach (var n in xml.XPathSelectElements("//node/node"))
		{
			var result = await FindDbusInterface(serviceName, objectPath.Length == 1 ? "/" + n.Attribute("name").Value : objectPath + "/" + n.Attribute("name").Value, match);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	public static async Task ActivateSystemTrayItemAsync(StatusNotifierItem statusNotifierItem, int x, int y)
	{
		var itemProxy = Connection.Session.CreateProxy<IStatusNotifierItem>(statusNotifierItem.Object.ServiceName, statusNotifierItem.Object.ObjectPath);
		await itemProxy.ActivateAsync(x, y);
	}

	public static async Task ContextMenuAsync(StatusNotifierItem statusNotifierItem, int x, int y)
	{
		var itemProxy = Connection.Session.CreateProxy<IStatusNotifierItem>(statusNotifierItem.Object.ServiceName, statusNotifierItem.Object.ObjectPath);
		await itemProxy.ContextMenuAsync(x, y);
	}

	public static async Task SecondaryActivateAsync(StatusNotifierItem statusNotifierItem, int x, int y)
	{
		var itemProxy = Connection.Session.CreateProxy<IStatusNotifierItem>(statusNotifierItem.Object.ServiceName, statusNotifierItem.Object.ObjectPath);
		await itemProxy.SecondaryActivateAsync(x, y);
	}

	public static async Task<DbusMenuItem> GetMenuItems(StatusNotifierItem statusNotifierItem)
	{
		var menuProxy = Connection.Session.CreateProxy<IDbusmenu>(statusNotifierItem.Menu.ServiceName, statusNotifierItem.Menu.ObjectPath);
		var dbusResult = await menuProxy.GetLayoutAsync(0, -1, Array.Empty<string>());
		return DbusMenuItem.From(dbusResult.layout);
	}

	public static async Task ClickedItem(StatusNotifierItem statusNotifierItem, int id)
	{
		var menuProxy = Connection.Session.CreateProxy<IDbusmenu>(statusNotifierItem.Menu.ServiceName, statusNotifierItem.Menu.ObjectPath);
		await menuProxy.EventAsync(id, "clicked", new byte[1], 0);
	}

	private static async Task<T> TryGetProp<T>(IStatusNotifierItem item, string prop)
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
