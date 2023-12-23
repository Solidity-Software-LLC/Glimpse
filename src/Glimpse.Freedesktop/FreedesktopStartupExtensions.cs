using Autofac;
using Glimpse.Freedesktop.DBus;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Freedesktop.DBus.Introspection;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Freedesktop.Notifications;
using Glimpse.Freedesktop.SystemTray;
using Glimpse.Redux.Effects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus.Protocol;

namespace Glimpse.Freedesktop;

public static class FreedesktopStartupExtensions
{
	public static async Task UseFreedesktop(this IHost host, string installationPath)
	{
		var container = host.Services;
		var dbusConnections = container.GetRequiredService<DBusConnections>();
		await dbusConnections.Session.ConnectAsync();
		await dbusConnections.System.ConnectAsync();
		await container.GetRequiredService<FreeDesktopService>().InitializeAsync(dbusConnections);
		await container.GetRequiredService<DBusSystemTrayService>().InitializeAsync();
		await container.GetRequiredService<NotificationsService>().InitializeAsync();
		await container.GetRequiredService<XSessionManager>().Register(installationPath);
	}

	public static void AddFreedesktop(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterInstance(SystemTrayItemStateReducers.Reducers);
		containerBuilder.RegisterInstance(Reducers.AllReducers);
		containerBuilder.RegisterType<SystemTrayItemStateEffects>().As<IEffectsFactory>();
		containerBuilder.RegisterType<FreeDesktopService>().SingleInstance();
		containerBuilder.RegisterType<DBusSystemTrayService>();
		containerBuilder.RegisterType<IntrospectionService>();
		containerBuilder.RegisterType<OrgFreedesktopAccounts>().SingleInstance();
		containerBuilder.RegisterType<OrgKdeStatusNotifierWatcher>().SingleInstance();
		containerBuilder.RegisterType<OrgFreedesktopNotifications>().SingleInstance();
		containerBuilder.RegisterType<NotificationsService>().SingleInstance();
		containerBuilder.RegisterType<XSessionManager>().SingleInstance();
		containerBuilder.Register(c => new OrgXfceSessionClient(c.Resolve<DBusConnections>().Session, "org_glimpse")).SingleInstance();
		containerBuilder.Register(c => new OrgFreedesktopDBus(c.Resolve<DBusConnections>().Session, Connection.DBusServiceName, Connection.DBusObjectPath)).SingleInstance();
		containerBuilder.Register(c => new OrgXfceSessionManager(c.Resolve<DBusConnections>().Session)).SingleInstance();
		containerBuilder.RegisterInstance(new DBusConnections() { Session = new Connection(Address.Session!), System = new Connection(Address.System!), }).ExternallyOwned();
	}
}
