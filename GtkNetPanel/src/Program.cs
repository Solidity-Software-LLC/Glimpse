using System.Reactive.Subjects;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Fluxor;
using GLib;
using GtkNetPanel.Components;
using GtkNetPanel.Components.ApplicationBar;
using GtkNetPanel.Components.SystemTray;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.SystemTray;
using GtkNetPanel.Services.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus;
using Application = Gtk.Application;

namespace GtkNetPanel;

public static class Program
{
	[STAThread]
	public static async Task<int> Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);

		var builder = Host.CreateDefaultBuilder(args)
			.UseServiceProviderFactory(new AutofacServiceProviderFactory(containerBuilder =>
			{
				containerBuilder.RegisterType<SharpPanel>();
				containerBuilder.RegisterType<SystemTrayBox>();

				containerBuilder.RegisterType<ApplicationBarView>();
				containerBuilder.RegisterType<ApplicationBarView>();
				containerBuilder.RegisterType<ApplicationBarController>();
				containerBuilder.RegisterType<ApplicationBarViewModel>();
				containerBuilder.RegisterType<BehaviorSubject<ApplicationBarViewModel>>()
					.As<IObservable<ApplicationBarViewModel>>()
					.As<BehaviorSubject<ApplicationBarViewModel>>()
					.InstancePerMatchingLifetimeScope("panel");

				containerBuilder.RegisterType<DBusSystemTrayService>();
				containerBuilder.RegisterType<IntrospectionService>();
				containerBuilder.RegisterType<TasksService>();
				containerBuilder.RegisterInstance(Connection.Session).ExternallyOwned();
				containerBuilder.Register(_ => new Application("org.SharpPanel", ApplicationFlags.None));
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

		var tasksService = host.Services.GetRequiredService<TasksService>();
		tasksService.Initialize();

		var watcher = host.Services.GetRequiredService<DBusSystemTrayService>();
		await watcher.Initialize();
		await host.RunAsync();
		return 0;
	}
}
