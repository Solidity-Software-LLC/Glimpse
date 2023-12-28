using System.Collections.Immutable;
using System.Reactive.Linq;
using Autofac;
using Autofac.Features.AttributeFilters;
using GLib;
using Glimpse.Common.System.Reactive;
using Glimpse.Configuration;
using Glimpse.Redux;
using Glimpse.Redux.Effects;
using Glimpse.UI.Components;
using Glimpse.UI.Components.Calendar;
using Glimpse.UI.Components.StartMenu;
using Glimpse.UI.Components.StartMenu.Window;
using Glimpse.UI.Components.SystemTray;
using Glimpse.UI.Components.Taskbar;
using Glimpse.UI.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application = Gtk.Application;
using DateTime = System.DateTime;
using Task = System.Threading.Tasks.Task;
using Unit = System.Reactive.Unit;

namespace Glimpse.UI;

public static class UIStartupExtensions
{
	public static Task UseGlimpseUI(this IHost host)
	{
		var store = host.Services.GetRequiredService<ReduxStore>();
		var configurationService = host.Services.GetRequiredService<ConfigurationService>();

		configurationService.ConfigurationUpdated.WithLatestFrom(store.Select(UISelectors.Root)).Subscribe(t =>
		{
			var (config, s) = t;
			var slots = config.Taskbar.PinnedLaunchers.Select(l => new SlotRef() { PinnedDesktopFileId = l }).ToImmutableList();
			store.Dispatch(new UpdateTaskbarSlotOrderingBulkAction() { Slots = slots });
		});

		return Task.CompletedTask;
	}
	public static void AddGlimpseUI(this IHostApplicationBuilder builder)
	{
		builder.Services.AddHostedService<GlimpseGtkApplication>();
	}

	public static void AddGlimpseUI(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterType<Panel>().WithAttributeFiltering();
		containerBuilder.RegisterType<GlimpseGtkApplication>().SingleInstance();
		containerBuilder.RegisterType<SystemTrayBox>();
		containerBuilder.RegisterType<TaskbarView>();
		containerBuilder.RegisterType<StartMenuLaunchIcon>();
		containerBuilder.RegisterType<StartMenuWindow>().SingleInstance();
		containerBuilder.RegisterType<CalendarWindow>().SingleInstance().WithAttributeFiltering();
		containerBuilder.RegisterInstance(UIReducers.AllReducers);
		containerBuilder.RegisterType<UIEffects>().As<IEffectsFactory>();
		containerBuilder.RegisterType<GlimpseGtkApplication>().SingleInstance();

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
