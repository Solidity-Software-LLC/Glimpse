using System.Reactive.Linq;
using Autofac;
using Autofac.Features.AttributeFilters;
using GLib;
using Glimpse.Common.System.Reactive;
using Glimpse.StartMenu;
using Glimpse.Taskbar;
using Glimpse.UI.Components;
using Glimpse.UI.Components.SidePane;
using Glimpse.UI.Components.SidePane.Calendar;
using Glimpse.UI.Components.SystemTray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application = Gtk.Application;
using DateTime = System.DateTime;
using Task = System.Threading.Tasks.Task;
using Unit = System.Reactive.Unit;

namespace Glimpse.UI;

public static class UIStartupExtensions
{
	public static async Task UseGlimpseUI(this IHost host)
	{
		await host.UseTaskbar();
		await host.UseSystemTray();
		await host.UseStartMenu();
	}

	public static void AddGlimpseUI(this IHostApplicationBuilder builder)
	{
		builder.Services.AddHostedService<GlimpseGtkApplication>();
	}

	public static void AddGlimpseUI(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterType<Panel>().WithAttributeFiltering();
		containerBuilder.RegisterType<GlimpseGtkApplication>().SingleInstance();
		containerBuilder.RegisterType<StartMenuDemands>().As<IStartMenuDemands>().SingleInstance();
		containerBuilder.RegisterType<CalendarWindow>().SingleInstance().WithAttributeFiltering();
		containerBuilder.RegisterType<SidePaneWindow>().SingleInstance().WithAttributeFiltering();
		containerBuilder.AddTaskbar();
		containerBuilder.AddSystemTray();
		containerBuilder.AddStartMenu();

		containerBuilder
			.Register(c =>
			{
				var host = c.Resolve<IHostApplicationLifetime>();
				var shuttingDown = Observable.Create<Unit>(obs => host.ApplicationStopping.Register(() => obs.OnNext(Unit.Default)));
				return TimerFactory.OneSecondTimer.TakeUntil(shuttingDown).Publish().AutoConnect();
			})
			.Keyed<IObservable<DateTime>>(Timers.OneSecond)
			.SingleInstance();

		containerBuilder
			.Register(_ =>
			{
				var app = new Application("org.glimpse", ApplicationFlags.None);
				app.AddAction(new SimpleAction("OpenStartMenu", null));
				app.AddAction(new SimpleAction("LoadPanels", null));
				return app;
			})
			.SingleInstance();
	}
}
