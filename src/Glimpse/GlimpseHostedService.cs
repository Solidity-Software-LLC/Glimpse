using System.Reactive.Linq;
using Glimpse.Extensions.Redux;
using Glimpse.Extensions.Redux.Effects;
using Glimpse.Services.Configuration;
using Glimpse.Services.DBus;
using Glimpse.Services.FreeDesktop;
using Glimpse.Services.SystemTray;
using Glimpse.Services.X11;
using Microsoft.Extensions.Hosting;

namespace Glimpse;

public class GlimpseHostedService(
	GlimpseGtkApplication application,
	ReduxStore store,
	Connections connections,
	FreeDesktopService freeDesktopService,
	ConfigurationService configurationService,
	DBusSystemTrayService dBusSystemTrayService,
	XLibAdaptorService xLibAdaptorService,
	IEffectsFactory[] effectFactories)
		: IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		try
		{
			var effects = effectFactories
				.SelectMany(e => e.Create())
				.Select(oldEffect => new Effect()
				{
					Run = _ => oldEffect.Run(store).Do(_ => { }, exception => Console.WriteLine(exception)),
					Config = oldEffect.Config
				})
				.ToArray();

			store.RegisterEffects(effects);

			await store.Dispatch(new InitializeStoreAction());
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
