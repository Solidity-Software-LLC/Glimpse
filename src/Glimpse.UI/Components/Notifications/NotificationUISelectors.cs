using System.Collections.Immutable;
using Glimpse.Common.System;
using Glimpse.Freedesktop;
using Glimpse.Freedesktop.Notifications;
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

public static class NotificationUISelectors
{
	public static readonly ISelector<NotificationsViewModel> ViewModel = SelectorFactory.CreateSelector(
		NotificationSelectors.NotificationsState,
		FreedesktopSelectors.AllDesktopFiles,
		(notifications, desktopFiles) =>
		{
			return new NotificationsViewModel()
			{
				Notifications = notifications.ById.Values.Select(n =>
				{
					var appIcon = desktopFiles.FirstOrDefault(d => d.Name == n.AppName)?.IconName;
					appIcon = appIcon.Or(n.AppIcon, GtkExtensions.MissingIconName);

					var notification = new NotificationViewModel()
					{
						Id = n.Id,
						AppName = n.AppName,
						Body = n.Body,
						Summary = n.Summary,
						Image = new ImageViewModel() { Image = n.Image, IconNameOrPath = n.Image == null ? n.AppIcon.Or("dialog-information-symbolic") : "" },
						AppIcon = new ImageViewModel() { IconNameOrPath = appIcon },
						Actions = n.Actions,
					};

					return notification;
				}).ToImmutableList()
			};
		});
}
