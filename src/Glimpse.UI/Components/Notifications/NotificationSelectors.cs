using System.Collections.Immutable;
using Glimpse.Common.System;
using Glimpse.Freedesktop;
using Glimpse.Redux.Selectors;
using Glimpse.UI.State;

namespace Glimpse.UI.Components.Notifications;

public record NotificationViewModel
{
	public uint Id { get; set; }
	public string Body { get; set; }
	public string Summary { get; set; }
	public string AppName { get; set; }
	public ImageViewModel Image { get; set; }
	public ImageViewModel AppIcon { get; set; }
	public string[] Actions { get; set; }
}

public class NotificationsViewModel
{
	public ImmutableList<NotificationViewModel> Notifications { get; set; }
}

public static class NotificationSelectors
{
	public static readonly ISelector<NotificationsViewModel> ViewModel = SelectorFactory.CreateSelector(
		FreedesktopSelectors.NotificationsState,
		FreedesktopSelectors.AllDesktopFiles,
		(notifications, desktopFiles) =>
		{
			return new NotificationsViewModel()
			{
				Notifications = notifications.ById.Values.Select(n =>
				{
					var appIcon = desktopFiles.FirstOrDefault(d => d.Name == n.FreedesktopNotification.AppName)?.IconName;
					appIcon = appIcon.Or(n.FreedesktopNotification.AppIcon, GtkExtensions.MissingIconName);

					var notification = new NotificationViewModel()
					{
						Id = n.FreedesktopNotification.Id,
						AppName = n.FreedesktopNotification.AppName,
						Body = n.FreedesktopNotification.Body,
						Summary = n.FreedesktopNotification.Summary,
						Image = new ImageViewModel() { Image = n.FreedesktopNotification.Image, IconNameOrPath = n.FreedesktopNotification.Image == null ? n.FreedesktopNotification.AppIcon.Or("dialog-information-symbolic") : "" },
						AppIcon = new ImageViewModel() { IconNameOrPath = appIcon },
						Actions = n.FreedesktopNotification.Actions,
					};

					return notification;
				}).ToImmutableList()
			};
		});
}
