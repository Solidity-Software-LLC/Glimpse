using System.Reactive.Subjects;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fluxor;
using GLib;
using GtkNetPanel.Components;
using GtkNetPanel.Components.ApplicationBar;
using GtkNetPanel.Components.StartMenu;
using GtkNetPanel.Components.SystemTray;
using GtkNetPanel.Services.Configuration;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.Services.SystemTray;
using GtkNetPanel.Services.X11;
using GtkNetPanel.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus;
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
				containerBuilder.RegisterType<ApplicationBarView>();
				containerBuilder.RegisterType<ApplicationBarController>();
				containerBuilder.RegisterType<ApplicationBarViewModel>();
				containerBuilder.RegisterType<StartMenuLaunchIcon>();
				containerBuilder.RegisterType<FreeDesktopService>().SingleInstance();
				containerBuilder.RegisterType<X11DisplayServer>().As<X11DisplayServer>().As<IDisplayServer>().SingleInstance();
				containerBuilder.RegisterType<DBusSystemTrayService>();
				containerBuilder.RegisterType<IntrospectionService>();
				containerBuilder.RegisterType<ConfigurationService>().SingleInstance();
				containerBuilder.RegisterType<XLibAdaptorService>().SingleInstance();
				containerBuilder.RegisterType<RootStateSelectors>().SingleInstance();
				containerBuilder.RegisterType<StartMenuSelectors>().SingleInstance();
				containerBuilder.RegisterInstance(Connection.Session).ExternallyOwned();
				containerBuilder.Register(_ => new Application("org.SharpPanel", ApplicationFlags.None)).SingleInstance();
			}))
			.ConfigureServices(services =>
			{
				services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly).WithLifetime(StoreLifetime.Singleton));
				services.AddHostedService<GtkApplicationHostedService>();
			})
			.UseConsoleLifetime();

		var host = builder.Build();
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

		var watcher = host.Services.GetRequiredService<DBusSystemTrayService>();
		await watcher.Initialize();
		await host.RunAsync();
		return 0;
	}
}
