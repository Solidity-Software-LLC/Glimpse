using Autofac;
using Glimpse.Freedesktop.DBus;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Freedesktop.DBus.Introspection;
using Glimpse.Freedesktop.DesktopEntries;
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
		await container.GetRequiredService<AccountService>().InitializeAsync(dbusConnections);
		await container.GetRequiredService<XSessionManager>().Register(installationPath);
		await host.UseDesktopFiles();
	}

	public static void AddFreedesktop(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterInstance(AccountReducers.AllReducers);
		containerBuilder.RegisterType<AccountService>().SingleInstance();
		containerBuilder.RegisterType<IntrospectionService>();
		containerBuilder.RegisterType<OrgFreedesktopAccounts>().SingleInstance();
		containerBuilder.RegisterType<OrgKdeStatusNotifierWatcher>().SingleInstance();
		containerBuilder.RegisterType<XSessionManager>().SingleInstance();
		containerBuilder.Register(c => new OrgXfceSessionClient(c.Resolve<DBusConnections>().Session, "org_glimpse")).SingleInstance();
		containerBuilder.Register(c => new OrgFreedesktopDBus(c.Resolve<DBusConnections>().Session, Connection.DBusServiceName, Connection.DBusObjectPath)).SingleInstance();
		containerBuilder.Register(c => new OrgXfceSessionManager(c.Resolve<DBusConnections>().Session)).SingleInstance();
		containerBuilder.RegisterInstance(new DBusConnections() { Session = new Connection(Address.Session!), System = new Connection(Address.System!), }).ExternallyOwned();
		containerBuilder.AddDesktopFiles();
	}
}
