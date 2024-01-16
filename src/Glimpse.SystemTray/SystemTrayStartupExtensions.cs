using Autofac;
using Glimpse.Freedesktop;
using Glimpse.Freedesktop.SystemTray;
using Glimpse.Redux.Effects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Glimpse.UI.Components.SystemTray;

public static class SystemTrayStartupExtensions
{
	public static async Task UseSystemTray(this IHost host)
	{
		var container = host.Services;
		await container.GetRequiredService<DBusSystemTrayService>().InitializeAsync();
	}

	public static void AddSystemTray(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterInstance(SystemTrayItemStateReducers.Reducers);
		containerBuilder.RegisterType<SystemTrayItemStateEffects>().As<IEffectsFactory>();
		containerBuilder.RegisterType<DBusSystemTrayService>();
		containerBuilder.RegisterType<SystemTrayBox>();
	}
}
