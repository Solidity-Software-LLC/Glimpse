using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Xml.Linq;
using System.Xml.XPath;
using GtkNetPanel.DBus.Introspection;
using GtkNetPanel.DBus.Menu;
using GtkNetPanel.DBus.StatusNotifierItem;
using Tmds.DBus;

namespace GtkNetPanel.DBus.StatusNotifierWatcher;

public class StatusNotifierWatcherService
{
	private const string StatusNotifierObjectPath = "/StatusNotifierWatcher";
	private readonly Connection _connection;
	private readonly object _lock = new();
	private readonly BehaviorSubject<ImmutableList<DbusStatusNotifierItem>> _statusNotifierItemsSubject = new(ImmutableList<DbusStatusNotifierItem>.Empty);
	private ImmutableList<DbusStatusNotifierItem> _statusNotifierItems = ImmutableList<DbusStatusNotifierItem>.Empty;

	public StatusNotifierWatcherService(Connection connection = null) => _connection = connection ?? Connection.Session;
	public IObservable<ImmutableList<DbusStatusNotifierItem>> StatusNotifierItems => _statusNotifierItemsSubject;

	public void Connect()
	{
		var watcher = _connection.CreateProxy<IStatusNotifierWatcher>(IStatusNotifierWatcher.DbusInterfaceName, StatusNotifierObjectPath);

		watcher.WatchStatusNotifierItemRegisteredAsync(async s =>
			{
				var obj = await FindStatusNotifierItem(s);
				var item = await CreateStatusNotifierItem(obj);

				lock (_lock)
				{
					_statusNotifierItems = _statusNotifierItems.Add(item);
					_statusNotifierItemsSubject.OnNext(_statusNotifierItems);
				}
			},
			exception => { Console.WriteLine(exception); });

		watcher.WatchStatusNotifierItemUnregisteredAsync(objPath =>
			{
				var serviceName = objPath.RemoveObjectPath();
				var serviceToRemove = _statusNotifierItems.FirstOrDefault(s => s.Object.ServiceName == serviceName);

				lock (_lock)
				{
					_statusNotifierItems = _statusNotifierItems.Remove(serviceToRemove);
					_statusNotifierItemsSubject.OnNext(_statusNotifierItems);
				}
			},
			exception => { Console.WriteLine(exception); });
	}

	public async Task LoadTrayItems()
	{
		var results = new List<DbusStatusNotifierItem>();
		var watcher = _connection.CreateProxy<IStatusNotifierWatcher>(IStatusNotifierWatcher.DbusInterfaceName, StatusNotifierObjectPath);

		foreach (var item in await watcher.GetRegisteredStatusNotifierItemsAsync())
		{
			var endpoint = await FindStatusNotifierItem(item);
			if (endpoint == null)
			{
				continue;
			}

			results.Add(await CreateStatusNotifierItem(endpoint));
		}

		lock (_lock)
		{
			_statusNotifierItems = _statusNotifierItems.AddRange(results.DistinctBy(r => r.Properties.Id));
			_statusNotifierItemsSubject.OnNext(_statusNotifierItems);
		}
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
			var dbusMenuObject = await FindDbusInterface(endpoint.ServiceName, menuPath, p => p == IDbusmenu.DbusInterfaceName);
			statusItem = statusItem with { Menu = dbusMenuObject };
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
