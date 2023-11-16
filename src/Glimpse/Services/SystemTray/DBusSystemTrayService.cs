using System.Reactive.Linq;
using Fluxor;
using Glimpse.Extensions;
using Glimpse.Services.DBus;
using Glimpse.Services.DBus.Core;
using Glimpse.Services.DBus.Interfaces;
using Glimpse.Services.DBus.Introspection;
using Glimpse.State.SystemTray;
using Tmds.DBus.Protocol;

namespace Glimpse.Services.SystemTray;

public class DBusSystemTrayService(
	Connections connections,
	IntrospectionService introspectionService,
	IDispatcher dispatcher,
	OrgKdeStatusNotifierWatcher watcher,
	OrgFreedesktopDBus orgFreedesktopDBus)
{
	private readonly Connection _connection = connections.Session;

	public async Task InitializeAsync()
	{
		watcher.Initialize();
		watcher.RegisterStatusNotifierHostAsync("org.freedesktop.StatusNotifierWatcher-panel");
		connections.Session.AddMethodHandler(watcher);

		watcher.ItemRegistered
			.Delay(TimeSpan.FromSeconds(1))
			.Select(s => Observable.FromAsync(() => CreateTrayItemState(s)).Take(1))
			.Concat()
			.Where(s => s != null)
			.Subscribe(s => dispatcher.Dispatch(new AddTrayItemAction() { ItemState = s }));

		await orgFreedesktopDBus.RequestNameAsync("org.kde.StatusNotifierWatcher", 0);
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
		var statusNotifierItemDesc = await introspectionService.FindDBusObjectDescription(serviceName, "/", i => i == "org.kde.StatusNotifierItem");
		var statusNotifierItemProxy = new OrgKdeStatusNotifierItem(_connection, statusNotifierItemDesc.ServiceName, statusNotifierItemDesc.ObjectPath);
		var menuObjectPath = await statusNotifierItemProxy.GetMenuPropertyAsync();
		var dbusMenuDescription = await introspectionService.FindDBusObjectDescription(statusNotifierItemDesc.ServiceName, menuObjectPath, p => p == "com.canonical.dbusmenu");
		var dbusMenuProxy = new ComCanonicalDbusmenu(_connection, dbusMenuDescription.ServiceName, dbusMenuDescription.ObjectPath);
		var dbusMenuLayout = await dbusMenuProxy.GetLayoutAsync(0, -1, Array.Empty<string>());
		var itemRemovedObservable = watcher.ItemRemoved.Where(s => s == serviceName).Take(1);

		statusNotifierItemProxy.PropertyChanged
			.TakeUntil(itemRemovedObservable)
			.Subscribe(props =>
			{
				dispatcher.Dispatch(new UpdateStatusNotifierItemPropertiesAction()
				{
					Properties = StatusNotifierItemProperties.From(props),
					ServiceName = serviceName
				});
			});

		dbusMenuProxy.LayoutUpdated
			.TakeUntil(itemRemovedObservable)
			.Throttle(TimeSpan.FromMilliseconds(250))
			.Subscribe(menu =>
			{
				dispatcher.Dispatch(new UpdateMenuLayoutAction { ServiceName = serviceName, RootMenuItem = DbusMenuItem.From(menu.layout) });
			});

		itemRemovedObservable
			.Subscribe(_ =>
			{
				dispatcher.Dispatch(new RemoveTrayItemAction { ServiceName = serviceName });
			});

		return new SystemTrayItemState()
		{
			Properties = StatusNotifierItemProperties.From(await statusNotifierItemProxy.GetAllPropertiesAsync()),
			StatusNotifierItemDescription = statusNotifierItemDesc,
			DbusMenuDescription = dbusMenuDescription,
			RootMenuItem = DbusMenuItem.From(dbusMenuLayout.layout)
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
