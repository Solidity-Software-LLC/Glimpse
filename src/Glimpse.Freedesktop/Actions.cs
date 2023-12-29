using System.Collections.Immutable;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Freedesktop.DesktopEntries;

namespace Glimpse.Freedesktop;

public class UpdateDesktopFilesAction
{
	public ImmutableList<DesktopFile> DesktopFiles { get; init; }
}

public class UpdateUserAction
{
	public string UserName { get; init; }
	public string IconPath { get; init; }
}

public record AddNotificationAction(FreedesktopNotification Notification);
public record NotificationTimerExpiredAction(uint NotificationId);
public record CloseNotificationAction(uint NotificationId);
