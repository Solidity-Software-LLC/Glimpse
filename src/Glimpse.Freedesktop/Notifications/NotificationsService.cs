using System.Reactive.Linq;
using Glimpse.Common.System.Reactive;
using Glimpse.Freedesktop.DBus;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Redux;

namespace Glimpse.Freedesktop.Notifications;

public enum NotificationCloseReason : int
{
	Expired = 1,
	Dismissed = 2,
	CloseNotification = 3,
	Undefined = 4
}

public class NotificationsService(
	OrgFreedesktopNotifications freedesktopNotifications,
	DBusConnections dBusConnections,
	OrgFreedesktopDBus orgFreedesktopDBus,
	ReduxStore store)
{
	public async Task InitializeAsync()
	{
		dBusConnections.Session.AddMethodHandler(freedesktopNotifications);
		await orgFreedesktopDBus.RequestNameAsync("org.freedesktop.Notifications", 0);

		freedesktopNotifications.CloseNotificationRequested.Subscribe(id =>
		{
			freedesktopNotifications.EmitNotificationClosed(id, (int) NotificationCloseReason.CloseNotification);
			store.Dispatch(new CloseNotificationAction(id));
		});

		freedesktopNotifications.Notifications.Subscribe(n =>
		{
			store.Dispatch(new AddNotificationAction(n));
		});

		store
			.Select(FreedesktopSelectors.NotificationsState)
			.Select(s => s.ById.Values)
			.UnbundleMany(n => n.Id)
			.RemoveIndex()
			.Subscribe(obs =>
			{
				obs.Take(1).Subscribe(n =>
				{
					Observable.Timer(n.FreedesktopNotification.Duration)
						.TakeUntil(obs.TakeLast(1))
						.Take(1)
						.Subscribe(_ =>
						{
							freedesktopNotifications.EmitNotificationClosed(n.Id, (int) NotificationCloseReason.Expired);
							store.Dispatch(new NotificationTimerExpiredAction(n.Id));
						});
				});
		});
	}

	public void ActionInvoked(uint notificationId, string action)
	{
		freedesktopNotifications.EmitActionInvoked(notificationId, action);
	}

	public void DismissNotification(uint notificationId)
	{
		freedesktopNotifications.EmitNotificationClosed(notificationId, (int) NotificationCloseReason.Dismissed);
		store.Dispatch(new CloseNotificationAction(notificationId));
	}
}
