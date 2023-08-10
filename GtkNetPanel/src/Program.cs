using Fluxor;
using GtkNetPanel.Components;
using GtkNetPanel.Components.Tray;
using GtkNetPanel.Services.DBus;
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
				services.AddTransient<SystemTray>();
				services.AddSingleton<DbusSystemTrayService>();
				services.AddSingleton(Connection.Session);
				services.AddHostedService<GtkApplicationHostedService>();
			})
			.UseConsoleLifetime();

		var host = builder.Build();
		var store = host.Services.GetRequiredService<IStore>();
		await store.InitializeAsync();

		var watcher = host.Services.GetRequiredService<DbusSystemTrayService>();
		watcher.Connect();
		await watcher.LoadTrayItems();
		await host.RunAsync();
		return 0;
	}
}
