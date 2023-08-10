using Fluxor;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.DBus.Menu;
using GtkNetPanel.Services.DBus.StatusNotifierItem;
using GtkNetPanel.Services.DBus.StatusNotifierWatcher;
using GtkNetPanel.State;
using Tmds.DBus;

namespace GtkNetPanel.Services.DBus;

public class DBusSystemTrayService
{
	private const string StatusNotifierObjectPath = "/StatusNotifierWatcher";
	private readonly Connection _connection;
	private readonly IntrospectionService _introspectionService;
	private readonly IDispatcher _dispatcher;
	private IStatusNotifierWatcher _watcherProxy;

	public DBusSystemTrayService(Connection connection, IntrospectionService introspectionService, IDispatcher dispatcher)
	{
		_introspectionService = introspectionService;
		_dispatcher = dispatcher;
		_connection = connection ?? Connection.Session;
	}

	public async Task Initialize()
	{
		_watcherProxy = _connection.CreateProxy<IStatusNotifierWatcher>(IStatusNotifierWatcher.DbusInterfaceName, StatusNotifierObjectPath);

		// Need to subscribe to item property changes

		await _watcherProxy.WatchStatusNotifierItemRegisteredAsync(async s =>
			{
				_dispatcher.Dispatch(new AddTrayItemAction() { ItemState = await CreateTrayItemState(s) });
			},
			Console.WriteLine);

		await LoadTrayItems();
	}

	private async Task LoadTrayItems()
	{
		var results = new List<SystemTrayItemState>();
		var watcher = _connection.CreateProxy<IStatusNotifierWatcher>(IStatusNotifierWatcher.DbusInterfaceName, StatusNotifierObjectPath);

		foreach (var statusNotifierItemServiceName in await watcher.GetRegisteredStatusNotifierItemsAsync())
		{
			results.Add(await CreateTrayItemState(statusNotifierItemServiceName));
		}

		_dispatcher.Dispatch(new AddBulkTrayItemsAction() { Items = results });
	}

	private async Task<SystemTrayItemState> CreateTrayItemState(string serviceName)
	{
		var statusNotifierItemDesc = await _introspectionService.FindDBusObjectDescription(serviceName.RemoveObjectPath(), "/", i => i == IStatusNotifierItem.DbusInterfaceName);
		var statusNotifierItemProxy = _connection.CreateProxy<IStatusNotifierItem>(statusNotifierItemDesc.ServiceName, statusNotifierItemDesc.ObjectPath);
		var menuObjectPath = await statusNotifierItemProxy.TryGetAsync<string>("Menu") ?? (await statusNotifierItemProxy.TryGetAsync<ObjectPath>("Menu")).ToString();
		var dbusMenuDescription = await _introspectionService.FindDBusObjectDescription(statusNotifierItemDesc.ServiceName, menuObjectPath, p => p == IDbusmenu.DbusInterfaceName);
		var dbusMenuProxy = _connection.CreateProxy<IDbusmenu>(dbusMenuDescription.ServiceName, dbusMenuDescription.ObjectPath);
		var dbusMenuLayout = await dbusMenuProxy.GetLayoutAsync(0, -1, Array.Empty<string>());

		var layoutSubscription = await dbusMenuProxy.WatchLayoutUpdatedAsync(async _ =>
		{
			var updatedLayoutResult = await dbusMenuProxy.GetLayoutAsync(0, -1, Array.Empty<string>());
			var updatedRootMenuItem = DbusSystemTrayMenuItem.From(updatedLayoutResult.layout);
			_dispatcher.Dispatch(new UpdateMenuLayoutAction { ServiceName = serviceName, RootMenuItem = updatedRootMenuItem });
		}, Console.WriteLine);

		await _watcherProxy.WatchStatusNotifierItemUnregisteredAsync(objPath =>
		{
			if (serviceName != objPath.RemoveObjectPath())
			{
				return;
			}

			_dispatcher.Dispatch(new RemoveTrayItemAction { ServiceName = serviceName });
			layoutSubscription.Dispose();
		}, Console.WriteLine);

		return new SystemTrayItemState()
		{
			Properties = await statusNotifierItemProxy.GetAllAsync(),
			StatusNotifierItemDescription = statusNotifierItemDesc,
			DbusMenuDescription = dbusMenuDescription,
			RootSystemTrayMenuItem = DbusSystemTrayMenuItem.From(dbusMenuLayout.layout)
		};
	}

	public async Task ActivateSystemTrayItemAsync(DbusObjectDescription desc, int x, int y)
	{
		var proxy = _connection.CreateProxy<IStatusNotifierItem>(desc.ServiceName, desc.ObjectPath);
		await proxy.ActivateAsync(x, y);
	}

	public async Task ContextMenuAsync(DbusObjectDescription desc, int x, int y)
	{
		var proxy = _connection.CreateProxy<IStatusNotifierItem>(desc.ServiceName, desc.ObjectPath);
		await proxy.ContextMenuAsync(x, y);
	}

	public async Task SecondaryActivateAsync(DbusObjectDescription desc, int x, int y)
	{
		var proxy = _connection.CreateProxy<IStatusNotifierItem>(desc.ServiceName, desc.ObjectPath);
		await proxy.SecondaryActivateAsync(x, y);
	}

	public async Task ClickedItem(DbusObjectDescription desc, int id)
	{
		var proxy = _connection.CreateProxy<IDbusmenu>(desc.ServiceName, desc.ObjectPath);
		await proxy.EventAsync(id, "clicked", new byte[1], 0);
	}
}
