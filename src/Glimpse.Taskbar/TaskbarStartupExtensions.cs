using System.Collections.Immutable;
using System.Reactive.Linq;
using Autofac;
using Glimpse.Configuration;
using Glimpse.Redux;
using Glimpse.Redux.Effects;
using Glimpse.UI.Components.Taskbar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Glimpse.Taskbar;

public static class TaskbarStartupExtensions
{
	public static Task UseTaskbar(this IHost host)
	{
		var store = host.Services.GetRequiredService<ReduxStore>();
		var configurationService = host.Services.GetRequiredService<ConfigurationService>();

		configurationService.ConfigurationUpdated.WithLatestFrom(store.Select(TaskbarStateSelectors.Root)).Subscribe(t =>
		{
			var (config, s) = t;
			var slots = config.Taskbar.PinnedLaunchers.Select(l => new SlotRef() { PinnedDesktopFileId = l }).ToImmutableList();
			store.Dispatch(new UpdateTaskbarSlotOrderingBulkAction() { Slots = slots });
		});

		return Task.CompletedTask;
	}

	public static void AddTaskbar(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterType<TaskbarView>();
		containerBuilder.RegisterInstance(TaskbarReducers.AllReducers);
		containerBuilder.RegisterType<TaskbarEffects>().As<IEffectsFactory>();
	}
}
