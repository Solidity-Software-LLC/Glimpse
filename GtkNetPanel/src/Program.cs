using Fluxor;
using GtkNetPanel.Components;
using GtkNetPanel.Components.ApplicationBar;
using GtkNetPanel.Components.SystemTray;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.SystemTray;
using GtkNetPanel.Services.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus;

namespace GtkNetPanel;

public static class Program
{
	[STAThread]
	public static async Task<int> Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);

		var builder = Host.CreateDefaultBuilder(args)
			.ConfigureServices(services =>
			{
				services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly));
				services.AddTransient<SharpPanel>();
				services.AddTransient<SystemTrayBox>();
				services.AddTransient<ApplicationBarBox>();
				services.AddSingleton<DBusSystemTrayService>();
				services.AddSingleton<IntrospectionService>();
				services.AddSingleton<TasksService>();
				services.AddSingleton(Connection.Session);
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
