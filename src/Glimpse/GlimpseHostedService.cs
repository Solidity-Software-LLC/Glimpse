using Fluxor;
using Glimpse.Services.Configuration;
using Glimpse.Services.DBus;
using Glimpse.Services.FreeDesktop;
using Glimpse.Services.SystemTray;
using Glimpse.Services.X11;
using Microsoft.Extensions.Hosting;

namespace Glimpse;

public class GlimpseHostedService(
	GlimpseGtkApplication application,
	IStore store,
	Connections connections,
	FreeDesktopService freeDesktopService,
	ConfigurationService configurationService,
	DBusSystemTrayService dBusSystemTrayService,
	XLibAdaptorService xLibAdaptorService)
		: IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		try
		{
			await store.InitializeAsync();
			await connections.Session.ConnectAsync();
			await connections.System.ConnectAsync();
			await freeDesktopService.InitializeAsync(connections);
			await configurationService.InitializeAsync();
			await dBusSystemTrayService.InitializeAsync();
			await xLibAdaptorService.InitializeAsync();
			application.InitializeAsync();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		application.Shutdown();
		return Task.CompletedTask;
	}
}
