using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Glimpse.Freedesktop.Notifications;

public static class NotificationsStartupExtensions
{
	public static async Task UseNotifications(this IHost host)
	{
		var container = host.Services;
		await container.GetRequiredService<NotificationsService>().InitializeAsync();
	}

	public static void AddNotifications(this ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterInstance(NotificationsReducers.AllReducers);
		containerBuilder.RegisterType<OrgFreedesktopNotifications>().SingleInstance();
		containerBuilder.RegisterType<NotificationsService>().SingleInstance();
	}
}
