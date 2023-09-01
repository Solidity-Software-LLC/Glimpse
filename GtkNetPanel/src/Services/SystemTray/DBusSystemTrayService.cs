using System.Reactive.Linq;
using Fluxor;
using GtkNetPanel.Services.DBus.Core;
using GtkNetPanel.Services.DBus.Interfaces;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.DBus.StatusNotifierWatcher;
using GtkNetPanel.State;
using Tmds.DBus.Protocol;

namespace GtkNetPanel.Services.SystemTray;

public class DBusSystemTrayService
{
	private readonly Connection _connection;
	private readonly IntrospectionService _introspectionService;
	private readonly IDispatcher _dispatcher;
	private readonly StatusNotifierWatcher _watcher;

	public DBusSystemTrayService(Connection connection, IntrospectionService introspectionService, IDispatcher dispatcher, StatusNotifierWatcher watcher)
	{
		_introspectionService = introspectionService;
		_dispatcher = dispatcher;
		_connection = connection;
		_watcher = watcher;

		_watcher.RegisterStatusNotifierHostAsync("org.freedesktop.StatusNotifierWatcher-panel");
		_watcher.ItemRegistered
			.Select(s => Observable.FromAsync(() => CreateTrayItemState(s)).Take(1))
			.Concat()
			.Where(s => s != null)
			.Subscribe(s => _dispatcher.Dispatch(new AddTrayItemAction() { ItemState = s }));
	}

	private async Task<SystemTrayItemState> CreateTrayItemState(string statusNotifierObjectPath)
	{
		try
		{
			return await CreateTrayItemStateInternal(statusNotifierObjectPath);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}

		return null;
	}

	private async Task<SystemTrayItemState> CreateTrayItemStateInternal(string statusNotifierObjectPath)
	{
		var serviceName = statusNotifierObjectPath.RemoveObjectPath();
		var statusNotifierItemDesc = await _introspectionService.FindDBusObjectDescription(serviceName, "/", i => i == "org.kde.StatusNotifierItem");
		var statusNotifierItemProxy = new OrgKdeStatusNotifierItem(_connection, statusNotifierItemDesc.ServiceName, statusNotifierItemDesc.ObjectPath);
		var menuObjectPath = await statusNotifierItemProxy.GetMenuPropertyAsync();
		var dbusMenuDescription = await _introspectionService.FindDBusObjectDescription(statusNotifierItemDesc.ServiceName, menuObjectPath, p => p == "com.canonical.dbusmenu");
		var dbusMenuProxy = new ComCanonicalDbusmenu(_connection, dbusMenuDescription.ServiceName, dbusMenuDescription.ObjectPath);
		var dbusMenuLayout = await dbusMenuProxy.GetLayoutAsync(0, -1, Array.Empty<string>());
		var itemRemovedObservable = _watcher.ItemRemoved.Where(s => s == serviceName).Take(1);

		statusNotifierItemProxy.PropertyChanged
			.TakeUntil(itemRemovedObservable)
			.Subscribe(props =>
			{
				Console.WriteLine("Property updated: " + serviceName + " " + statusNotifierItemDesc.ObjectPath);
				_dispatcher.Dispatch(new UpdateStatusNotifierItemPropertiesAction()
				{
					Properties = StatusNotifierItemProperties.From(props),
					ServiceName = serviceName
				});
			});

		dbusMenuProxy.LayoutUpdated
			.TakeUntil(itemRemovedObservable)
			.Subscribe(menu =>
			{
				_dispatcher.Dispatch(new UpdateMenuLayoutAction { ServiceName = serviceName, RootMenuItem = DbusSystemTrayMenuItem.From(menu.layout) });
			});

		itemRemovedObservable
			.Subscribe(_ =>
			{
				_dispatcher.Dispatch(new RemoveTrayItemAction { ServiceName = serviceName });
			});

		return new SystemTrayItemState()
		{
			Properties = StatusNotifierItemProperties.From(await statusNotifierItemProxy.GetAllPropertiesAsync()),
			StatusNotifierItemDescription = statusNotifierItemDesc,
			DbusMenuDescription = dbusMenuDescription,
			RootSystemTrayMenuItem = DbusSystemTrayMenuItem.From(dbusMenuLayout.layout)
		};
	}

	public async Task ActivateSystemTrayItemAsync(DbusObjectDescription desc, int x, int y)
	{
		var item = new OrgKdeStatusNotifierItem(_connection, desc.ServiceName, desc.ObjectPath);
		await item.ActivateAsync(x, y);
	}

	public async Task ContextMenuAsync(DbusObjectDescription desc, int x, int y)
	{
		var item = new OrgKdeStatusNotifierItem(_connection, desc.ServiceName, desc.ObjectPath);
		await item.ContextMenuAsync(x, y);
	}

	public async Task SecondaryActivateAsync(DbusObjectDescription desc, int x, int y)
	{
		var item = new OrgKdeStatusNotifierItem(_connection, desc.ServiceName, desc.ObjectPath);
		await item.SecondaryActivateAsync(x, y);
	}

	public async Task ClickedItem(DbusObjectDescription desc, int id)
	{
		var menu = new ComCanonicalDbusmenu(_connection, desc.ServiceName, desc.ObjectPath);
		await menu.EventAsync(id, "clicked", new DBusVariantItem("y", new DBusByteItem(0)), 0);
	}
}
