using System.Collections.Immutable;
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
	public TimeSpan Duration { get; set; }
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
					var desktopFile = desktopFiles.FirstOrDefault(d => d.Name == n.FreedesktopNotification.AppName);

					return new NotificationViewModel()
					{
						Id = n.FreedesktopNotification.Id,
						AppName = n.FreedesktopNotification.AppName,
						Body = n.FreedesktopNotification.Body,
						Summary = n.FreedesktopNotification.Summary,
						Image = new ImageViewModel() { Image = n.FreedesktopNotification.Image },
						Duration = n.FreedesktopNotification.Duration,
						AppIcon = new ImageViewModel() { IconName = desktopFile?.IconName },
						Actions = n.FreedesktopNotification.Actions,
					};
				}).ToImmutableList()
			};
		});
}