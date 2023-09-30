using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fluxor;
using GLib;
using Glimpse.Components;
using Glimpse.Components.StartMenu;
using Glimpse.Components.SystemTray;
using Glimpse.Components.Taskbar;
using Glimpse.Services.Configuration;
using Glimpse.Services.DBus;
using Glimpse.Services.DBus.Interfaces;
using Glimpse.Services.DBus.Introspection;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.Services.SystemTray;
using Glimpse.Services.X11;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus.Protocol;
using Application = Gtk.Application;

namespace Glimpse;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);

		var builder = Host.CreateDefaultBuilder(args)
			.UseServiceProviderFactory(new AutofacServiceProviderFactory(containerBuilder =>
			{
				containerBuilder.RegisterType<Panel>();
				containerBuilder.RegisterType<SystemTrayBox>();
				containerBuilder.RegisterType<TaskbarView>();
				containerBuilder.RegisterType<StartMenuLaunchIcon>();
				containerBuilder.RegisterType<FreeDesktopService>().SingleInstance();
				containerBuilder.RegisterType<X11DisplayServer>().As<X11DisplayServer>().As<IDisplayServer>().SingleInstance();
				containerBuilder.RegisterType<DBusSystemTrayService>();
				containerBuilder.RegisterType<IntrospectionService>();
				containerBuilder.RegisterType<ConfigurationService>().SingleInstance();
				containerBuilder.RegisterType<XLibAdaptorService>().SingleInstance();
				containerBuilder.RegisterType<OrgFreedesktopAccounts>().SingleInstance();
				containerBuilder.RegisterType<OrgKdeStatusNotifierWatcher>().SingleInstance();
				containerBuilder.Register(c => new OrgFreedesktopDBus(c.Resolve<Connections>().Session, Connection.DBusServiceName, Connection.DBusObjectPath)).SingleInstance();
				containerBuilder.RegisterInstance(new Connections()
					{
						Session = new Connection(Address.Session!),
						System = new Connection(Address.System!),
					}).ExternallyOwned();
				containerBuilder.Register(_ =>
				{
					var app = new Application("org.solidity-software-llc.glimpse", ApplicationFlags.None);
					app.AddAction(new SimpleAction("OpenStartMenu", null));
					return app;
				}).SingleInstance();
			}))
			.ConfigureServices(services =>
			{
				services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly).WithLifetime(StoreLifetime.Singleton));
				services.AddHostedService<GtkApplicationHostedService>();
			})
			.UseConsoleLifetime();

		var host = builder.Build();

		var dbusConnection = host.Services.GetRequiredService<Connections>();
		await dbusConnection.Session.ConnectAsync();
		await dbusConnection.System.ConnectAsync();
		dbusConnection.Session.AddMethodHandler(host.Services.GetRequiredService<OrgKdeStatusNotifierWatcher>());

		var store = host.Services.GetRequiredService<IStore>();
		await store.InitializeAsync();

		var fdService = host.Services.GetRequiredService<FreeDesktopService>();
		fdService.Init(dbusConnection);

		var configService = host.Services.GetRequiredService<ConfigurationService>();
		configService.Initialize();

		var xService = host.Services.GetRequiredService<XLibAdaptorService>();
		xService.Initialize();

		var dbusInterface = host.Services.GetRequiredService<OrgFreedesktopDBus>();
		await dbusInterface.RequestNameAsync("org.kde.StatusNotifierWatcher", 0);

		await host.RunAsync();
		return 0;
	}
}
