using System.Xml.Linq;
using System.Xml.XPath;
using Fluxor;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.DBus.Menu;
using GtkNetPanel.Services.DBus.StatusNotifierItem;
using GtkNetPanel.Services.DBus.StatusNotifierWatcher;
using GtkNetPanel.State;
using Tmds.DBus;

namespace GtkNetPanel.Services.DBus;

public class DbusSystemTrayService
{
	private const string StatusNotifierObjectPath = "/StatusNotifierWatcher";
	private readonly Connection _connection;
	private readonly IDispatcher _dispatcher;

	public DbusSystemTrayService(Connection connection, IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
		_connection = connection ?? Connection.Session;
	}

	public void Connect()
	{
		var watcher = _connection.CreateProxy<IStatusNotifierWatcher>(IStatusNotifierWatcher.DbusInterfaceName, StatusNotifierObjectPath);

		// Need to subscribe to item property changes
		// Need to get the menu here and subscribe to changes there too
		// Handle unregistering too

		watcher.WatchStatusNotifierItemRegisteredAsync(async s =>
			{
				_dispatcher.Dispatch(new AddTrayItemAction() { ItemState = await CreateTrayItemState(s) });
			},
			Console.WriteLine);

		watcher.WatchStatusNotifierItemUnregisteredAsync(objPath =>
			{
				_dispatcher.Dispatch(new RemoveTrayItemAction() { ServiceName = objPath.RemoveObjectPath() });
			},
			Console.WriteLine);
	}

	public async Task LoadTrayItems()
	{
		var results = new List<TrayItemState>();
		var watcher = _connection.CreateProxy<IStatusNotifierWatcher>(IStatusNotifierWatcher.DbusInterfaceName, StatusNotifierObjectPath);

		foreach (var statusItems in await watcher.GetRegisteredStatusNotifierItemsAsync())
		{
			if (await FindStatusNotifierItem(statusItems) is { } endpoint)
			{
				results.Add(await CreateTrayItemState(endpoint.ServiceName));
			}
		}

		_dispatcher.Dispatch(new AddBulkTrayItemsAction() { Items = results });
	}

	private async Task<TrayItemState> CreateTrayItemState(string serviceName)
	{
		var status = await CreateStatusNotifierItem(await FindStatusNotifierItem(serviceName));
		var menuProxy = _connection.CreateProxy<IDbusmenu>(status.Menu.ServiceName, status.Menu.ObjectPath);
		var layoutResult = await menuProxy.GetLayoutAsync(0, -1, Array.Empty<string>());
		var rootMenuItem = DbusMenuItem.From(layoutResult.layout);
		return new TrayItemState() { Status = status, RootMenuItem = rootMenuItem };
	}

	private async Task<DbusObject> FindStatusNotifierItem(string serviceName)
	{
		var parsedServiceName = serviceName.RemoveObjectPath();
		return await FindDbusInterface(parsedServiceName, "/", i => i == IStatusNotifierItem.DbusInterfaceName);
	}

	private async Task<DbusStatusNotifierItem> CreateStatusNotifierItem(DbusObject endpoint)
	{
		var itemProxy = _connection.CreateProxy<IStatusNotifierItem>(endpoint.ServiceName, endpoint.ObjectPath);
		var statusItem = new DbusStatusNotifierItem { Properties = await itemProxy.GetAllAsync(), Object = endpoint };
		var menuPath = await TryGetProp<string>(itemProxy, "Menu") ?? (await TryGetProp<ObjectPath>(itemProxy, "Menu")).ToString();

		if (!string.IsNullOrEmpty(menuPath))
		{
			statusItem.Menu = await FindDbusInterface(endpoint.ServiceName, menuPath, p => p == IDbusmenu.DbusInterfaceName);
		}

		return statusItem;
	}

	private async Task<DbusObject> FindDbusInterface(string serviceName, string objectPath, Func<string, bool> match)
	{
		var introProxy = _connection.CreateProxy<IIntrospectable>(serviceName, objectPath);
		var rawXml = await introProxy.IntrospectAsync();
		var xml = XDocument.Parse(rawXml);

		foreach (var i in xml.XPathSelectElements("//node/interface"))
		{
			if (!match(i.Attribute("name").Value))
			{
				continue;
			}

			return new DbusObject
			{
				ServiceName = serviceName,
				ObjectPath = objectPath,
				Xml = rawXml,
				Interfaces = xml
					.XPathSelectElements("//interface")
					.Select(x => new DbusInterface { Name = x.Attribute("name").Value, Methods = x.XPathSelectElements("./method").Select(m => m.Attribute("name").Value).ToArray() }).ToList()
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

public static class StringExtensions
{
	public static string RemoveObjectPath(this string s) => s.Split('/', StringSplitOptions.RemoveEmptyEntries).First();
}
