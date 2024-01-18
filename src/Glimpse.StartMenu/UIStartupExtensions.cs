using Autofac;
using Glimpse.Redux.Effects;
using Glimpse.UI.Components.StartMenu;
using Glimpse.UI.Components.StartMenu.Window;
using Glimpse.UI.State;
using Microsoft.Extensions.Hosting;
using Task = System.Threading.Tasks.Task;

namespace Glimpse.UI;

public static class StartMenuStartupExtensions
{
	public static Task UseStartMenu(this IHost host)
	{
		return Task.CompletedTask;
	}

	public static void AddStartMenu(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterType<StartMenuLaunchIcon>();
		containerBuilder.RegisterType<StartMenuWindow>().SingleInstance();
		containerBuilder.RegisterInstance(UIReducers.AllReducers);
		containerBuilder.RegisterType<UIEffects>().As<IEffectsFactory>();
	}
}
