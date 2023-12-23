using Autofac;
using Glimpse.Redux.Effects;
using Glimpse.Xorg.State;
using Glimpse.Xorg.X11;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Task = System.Threading.Tasks.Task;

namespace Glimpse.Xorg;

public static class XorgStartupExtensions
{
	public static Task UseXorg(this IHost host)
	{
		return Task.CompletedTask;
	}

	public static void AddXorg(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterType<Effects>().As<IEffectsFactory>();
		containerBuilder.RegisterType<XLibAdaptorService>().SingleInstance();
		containerBuilder.RegisterType<X11DisplayServer>().As<X11DisplayServer>().As<IDisplayServer>().SingleInstance();
		containerBuilder.RegisterInstance(XorgReducers.Reducers);
	}

	public static void AddXorg(this IHostApplicationBuilder builder)
	{
		builder.Services.AddHostedService<XorgHostedService>();
	}
}
