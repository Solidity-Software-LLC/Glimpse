using System.Collections.Immutable;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Redux;
using Glimpse.Redux.Reducers;
using Glimpse.Redux.Selectors;

namespace Glimpse.Freedesktop.Notifications;

public record LoadNotificationHistoryAction(NotificationHistory History);
public record AddNotificationAction(FreedesktopNotification Notification);
public record NotificationTimerExpiredAction(uint NotificationId);
public record CloseNotificationAction(uint NotificationId);

public record NotificationHistoryApplication
{
	public string Name { get; set; }
}

public record NotificationHistory
{
	public ImmutableList<NotificationHistoryEntry> Notifications { get; set; } = ImmutableList<NotificationHistoryEntry>.Empty;
	public ImmutableList<NotificationHistoryApplication> KnownApplications { get; set; } = ImmutableList<NotificationHistoryApplication>.Empty;
}

public record NotificationHistoryEntry
{
	public string AppName { get; set; }
	public string AppIcon { get; set; }
	public string Summary { get; set; }
	public string Body { get; set; }
	public DateTime CreationDate { get; set; }
	public string DesktopEntry { get; set; }
	public string ImagePath { get; set; }
	public string BinaryImage { get; set; }
}

public static class NotificationSelectors
{
	public static readonly ISelector<NotificationHistory> NotificationHistory = SelectorFactory.CreateFeatureSelector<NotificationHistory>();
	public static readonly ISelector<DataTable<uint, FreedesktopNotification>> NotificationsState = SelectorFactory.CreateFeatureSelector<DataTable<uint, FreedesktopNotification>>();
}

internal class NotificationsReducers
{
	public static readonly FeatureReducerCollection AllReducers =
	[
		FeatureReducer.Build(new NotificationHistory())
			.On<LoadNotificationHistoryAction>((s, a) => a.History)
			.On<AddNotificationAction>((s, a) =>
			{
				var result = s;
				var scaledImage = a.Notification.Image?.ScaleToFit(34, 34);

				var notificationHistoryEntry = new NotificationHistoryEntry()
				{
					AppName = a.Notification.AppName,
					AppIcon = a.Notification.AppIcon,
					CreationDate = a.Notification.CreationDate,
					Body = a.Notification.Body,
					Summary = a.Notification.Summary,
					DesktopEntry = a.Notification.DesktopEntry,
					ImagePath = a.Notification.ImagePath,
					BinaryImage = Convert.ToBase64String(scaledImage?.Pixbuf?.SaveToBuffer("png") ?? Array.Empty<byte>())
				};

				if (result.KnownApplications.All(app => app.Name != a.Notification.AppName))
				{
					var app = new NotificationHistoryApplication() { Name = a.Notification.AppName };
					result = result with { KnownApplications = s.KnownApplications.Add(app) };
				}

				return result with
				{
					Notifications = result.Notifications
						.Where(x => x.CreationDate.AddDays(3) > DateTime.UtcNow)
						.Concat([notificationHistoryEntry])
						.ToImmutableList()
				};
			}),
		FeatureReducer.Build(new DataTable<uint, FreedesktopNotification>())
			.On<AddNotificationAction>((s, a) => s.UpsertOne(a.Notification))
			.On<NotificationTimerExpiredAction>((s, a) => s.Remove(a.NotificationId))
			.On<CloseNotificationAction>((s, a) => s.Remove(a.NotificationId))
	];
}
