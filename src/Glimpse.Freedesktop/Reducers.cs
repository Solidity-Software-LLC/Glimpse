using System.Collections.Immutable;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Redux;
using Glimpse.Redux.Reducers;

namespace Glimpse.Freedesktop;

public record AccountState
{
	public string UserName { get; init; }
	public string IconPath { get; init; }
	public virtual bool Equals(AccountState other) => ReferenceEquals(this, other);
}

public record NotificationsState
{
	public ImmutableList<NotificationState> Notifications = ImmutableList<NotificationState>.Empty;
}

public record NotificationState
{
	public FreedesktopNotification FreedesktopNotification { get; set; }
	public bool IsDismissed { get; set; }
	public DateTime CreationDateUtc { get; set; }
}

internal class Reducers
{
	public static readonly FeatureReducerCollection AllReducers = new()
	{
		FeatureReducer.Build(new NotificationsState())
			.On<AddNotificationAction>((s, a) => s with
			{
				Notifications = s.Notifications.Add(new () { FreedesktopNotification = a.Notification, CreationDateUtc = DateTime.UtcNow })
			})
			.On<NotificationTimerExpiredAction>((s, a) =>
			{
				var notificationToRemove = s.Notifications.FirstOrDefault(x => x.FreedesktopNotification.Id == a.NotificationId);
				if (notificationToRemove == null) return s;
				return s with { Notifications = s.Notifications.Remove(notificationToRemove) };
			})
			.On<CloseNotificationAction>((s, a) =>
			{
				var notificationToRemove = s.Notifications.FirstOrDefault(x => x.FreedesktopNotification.Id == a.NotificationId);
				if (notificationToRemove == null) return s;
				return s with { Notifications = s.Notifications.Remove(notificationToRemove) };
			}),
		FeatureReducer.Build(new DataTable<string, DesktopFile>())
			.On<UpdateDesktopFilesAction>((s, a) => s.UpsertMany(a.DesktopFiles)),
		FeatureReducer.Build(new AccountState())
			.On<UpdateUserAction>((s, a) => new AccountState { UserName = a.UserName, IconPath = a.IconPath }),
	};
}
