﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fluxor;
using GLib;
using GtkNetPanel.Components;
using GtkNetPanel.Components.StartMenu;
using GtkNetPanel.Components.SystemTray;
using GtkNetPanel.Components.Taskbar;
using GtkNetPanel.Services.Configuration;
using GtkNetPanel.Services.DBus.Interfaces;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.DBus.StatusNotifierWatcher;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.Services.SystemTray;
using GtkNetPanel.Services.X11;
using GtkNetPanel.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus.Protocol;
using Application = Gtk.Application;

namespace GtkNetPanel;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);

		var builder = Host.CreateDefaultBuilder(args)
			.UseServiceProviderFactory(new AutofacServiceProviderFactory(containerBuilder =>
			{
				containerBuilder.RegisterType<App>();
				containerBuilder.RegisterType<SystemTrayBox>();
				containerBuilder.RegisterType<TaskbarView>();
				containerBuilder.RegisterType<TaskbarSelectors>().SingleInstance();
				containerBuilder.RegisterType<StartMenuLaunchIcon>();
				containerBuilder.RegisterType<FreeDesktopService>().SingleInstance();
				containerBuilder.RegisterType<X11DisplayServer>().As<X11DisplayServer>().As<IDisplayServer>().SingleInstance();
				containerBuilder.RegisterType<DBusSystemTrayService>();
				containerBuilder.RegisterType<IntrospectionService>();
				containerBuilder.RegisterType<ConfigurationService>().SingleInstance();
				containerBuilder.RegisterType<XLibAdaptorService>().SingleInstance();
				containerBuilder.RegisterType<RootStateSelectors>().SingleInstance();
				containerBuilder.RegisterType<StartMenuSelectors>().SingleInstance();
				containerBuilder.RegisterType<StatusNotifierWatcher>().SingleInstance();
				containerBuilder.Register(c => new OrgFreedesktopDBus(c.Resolve<Connection>(), Connection.DBusServiceName, Connection.DBusObjectPath)).SingleInstance();
				containerBuilder.RegisterInstance(new Connection(new ClientConnectionOptions(Address.Session!) { })).ExternallyOwned();
				containerBuilder.Register(_ => new Application("org.SharpPanel", ApplicationFlags.None)).SingleInstance();
			}))
			.ConfigureServices(services =>
			{
				services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly).WithLifetime(StoreLifetime.Singleton));
				services.AddHostedService<GtkApplicationHostedService>();
			})
			.UseConsoleLifetime();

		var host = builder.Build();

		var dbusConnection = host.Services.GetRequiredService<Connection>();
		await dbusConnection.ConnectAsync();
		dbusConnection.AddMethodHandler(host.Services.GetRequiredService<StatusNotifierWatcher>());

		var store = host.Services.GetRequiredService<IStore>();
		await store.InitializeAsync();

		var fdService = host.Services.GetRequiredService<FreeDesktopService>();
		fdService.Init();

		var configService = host.Services.GetRequiredService<ConfigurationService>();
		configService.Initialize();

		var xService = host.Services.GetRequiredService<XLibAdaptorService>();
		xService.Initialize();

		var displayServer = host.Services.GetRequiredService<X11DisplayServer>();
		displayServer.Initialize();

		var dbusInterface = host.Services.GetRequiredService<OrgFreedesktopDBus>();
		await dbusInterface.RequestNameAsync("org.kde.StatusNotifierWatcher", 0);

		await host.RunAsync();
		return 0;
	}
}
