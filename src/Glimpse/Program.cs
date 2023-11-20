using System.Reactive;
using System.Reactive.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.AttributeFilters;
using GLib;
using Glimpse.Components;
using Glimpse.Components.Calendar;
using Glimpse.Components.StartMenu;
using Glimpse.Components.StartMenu.Window;
using Glimpse.Components.SystemTray;
using Glimpse.Components.Taskbar;
using Glimpse.Extensions.Redux;
using Glimpse.Extensions.Redux.Effects;
using Glimpse.Services;
using Glimpse.Services.Configuration;
using Glimpse.Services.DBus;
using Glimpse.Services.DBus.Interfaces;
using Glimpse.Services.DBus.Introspection;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.Services.SystemTray;
using Glimpse.Services.X11;
using Glimpse.State;
using Glimpse.State.SystemTray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus.Protocol;
using Application = Gtk.Application;
using DateTime = System.DateTime;
using Effects = Glimpse.State.Effects;

namespace Glimpse;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);

		var builder = Host.CreateApplicationBuilder(args);
		builder.ConfigureContainer(new AutofacServiceProviderFactory(ConfigureContainer));
		builder.Services.AddHostedService<GlimpseHostedService>();

		var host = builder.Build();
		await host.RunAsync();
		return 0;
	}

	private static void ConfigureContainer(ContainerBuilder containerBuilder)
	{
		containerBuilder
			.Register(c =>
			{
				var host = c.Resolve<IHostApplicationLifetime>();
				var shuttingDown = Observable.Create<Unit>(obs => host.ApplicationStopping.Register(() => obs.OnNext(Unit.Default)));
				return TimerFactory.OneSecondTimer.TakeUntil(shuttingDown).Publish().AutoConnect();
			})
			.Keyed<IObservable<DateTime>>(Timers.OneSecond)
			.SingleInstance();

		containerBuilder.RegisterType<ReduxStore>().SingleInstance();
		containerBuilder.RegisterInstance(AllReducers.Reducers);
		containerBuilder.RegisterInstance(SystemTrayItemStateReducers.Reducers);
		containerBuilder.RegisterType<Effects>().As<IEffectsFactory>();
		containerBuilder.RegisterType<SystemTrayItemStateEffects>().As<IEffectsFactory>();
		containerBuilder.RegisterType<Panel>().WithAttributeFiltering();
		containerBuilder.RegisterType<GlimpseGtkApplication>().SingleInstance();
		containerBuilder.RegisterType<SystemTrayBox>();
		containerBuilder.RegisterType<TaskbarView>();
		containerBuilder.RegisterType<StartMenuLaunchIcon>();
		containerBuilder.RegisterType<StartMenuWindow>().SingleInstance();
		containerBuilder.RegisterType<CalendarWindow>().SingleInstance().WithAttributeFiltering();
		containerBuilder.RegisterType<FreeDesktopService>().SingleInstance();
		containerBuilder.RegisterType<X11DisplayServer>().As<X11DisplayServer>().As<IDisplayServer>().SingleInstance();
		containerBuilder.RegisterType<DBusSystemTrayService>();
		containerBuilder.RegisterType<IntrospectionService>();
		containerBuilder.RegisterType<ConfigurationService>().SingleInstance();
		containerBuilder.RegisterType<XLibAdaptorService>().SingleInstance();
		containerBuilder.RegisterType<OrgFreedesktopAccounts>().SingleInstance();
		containerBuilder.RegisterType<OrgKdeStatusNotifierWatcher>().SingleInstance();
		containerBuilder.Register(c => new OrgFreedesktopDBus(c.Resolve<Connections>().Session, Connection.DBusServiceName, Connection.DBusObjectPath)).SingleInstance();
		containerBuilder.RegisterInstance(new Connections()
		{
			Session = new Connection(Address.Session!),
			System = new Connection(Address.System!),
		}).ExternallyOwned();
		containerBuilder.Register(_ =>
		{
			var app = new Application("org.solidity-software-llc.glimpse", ApplicationFlags.None);
			app.AddAction(new SimpleAction("OpenStartMenu", null));
			app.AddAction(new SimpleAction("LoadPanels", null));
			return app;
		}).SingleInstance();
	}
}
