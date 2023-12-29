using System.Reactive.Linq;
using Glimpse.Freedesktop.DBus;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Redux;

namespace Glimpse.Freedesktop.Notifications;

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

		freedesktopNotifications.Notifications.Subscribe(n =>
		{
			store.Dispatch(new AddNotificationAction(n));
			Observable.Timer(n.Duration).Take(1).Subscribe(_ => store.Dispatch(new NotificationTimerExpiredAction(n.Id)));
		});
	}

	public void CloseNotification(uint notificationId)
	{
		store.Dispatch(new CloseNotificationAction(notificationId));
	}
}
