using System.Reactive;
using System.Reactive.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.AttributeFilters;
using Fluxor;
using Fluxor.Selectors;
using GLib;
using Glimpse.Components;
using Glimpse.Components.Calendar;
using Glimpse.Components.StartMenu;
using Glimpse.Components.StartMenu.Window;
using Glimpse.Components.SystemTray;
using Glimpse.Components.Taskbar;
using Glimpse.Extensions.Fluxor;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tmds.DBus.Protocol;
using Application = Gtk.Application;
using DateTime = System.DateTime;

namespace Glimpse;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);

		var builder = Host.CreateDefaultBuilder(args)
			.UseServiceProviderFactory(new AutofacServiceProviderFactory(containerBuilder =>
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

				containerBuilder.RegisterType<Panel>().WithAttributeFiltering();
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
			}))
			.ConfigureServices(services =>
			{
				services.AddFluxor(o => o.ScanAssemblies(typeof(Program).Assembly).WithLifetime(StoreLifetime.Singleton));
				services.AddHostedService<GtkApplicationHostedService>();
			})
			.UseConsoleLifetime();

		var host = builder.Build();

		var dbusConnection = host.Services.GetRequiredService<Connections>();
		await dbusConnection.Session.ConnectAsync();
		await dbusConnection.System.ConnectAsync();
		dbusConnection.Session.AddMethodHandler(host.Services.GetRequiredService<OrgKdeStatusNotifierWatcher>());

		var store = host.Services.GetRequiredService<IStore>();
		await store.InitializeAsync();

		var dispatcher = host.Services.GetRequiredService<IDispatcher>();

		var fdService = host.Services.GetRequiredService<FreeDesktopService>();
		fdService.Init(dbusConnection);

		var configService = host.Services.GetRequiredService<ConfigurationService>();
		configService.Initialize();

		store.SubscribeSelector(TaskbarSelectors.Slots)
			.ToObservable()
			.Take(1)
			.Subscribe(slots => dispatcher.Dispatch(new UpdateTaskbarSlotOrderingBulkAction() { Slots = slots.Refs }));

		store.SubscribeSelector(TaskbarSelectors.SortedSlots)
			.ToObservable()
			.DistinctUntilChanged()
			.Subscribe(slots => dispatcher.Dispatch(new UpdateTaskbarSlotOrderingBulkAction() { Slots = slots.Refs }));

		var xService = host.Services.GetRequiredService<XLibAdaptorService>();
		xService.Initialize();

		var dbusInterface = host.Services.GetRequiredService<OrgFreedesktopDBus>();
		await dbusInterface.RequestNameAsync("org.kde.StatusNotifierWatcher", 0);

		await host.RunAsync();
		return 0;
	}
}
