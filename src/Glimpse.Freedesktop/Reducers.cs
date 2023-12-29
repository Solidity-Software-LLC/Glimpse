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

public record NotificationState : IKeyed<uint>
{
	public FreedesktopNotification FreedesktopNotification { get; set; }
	public bool IsDismissed { get; set; }
	public DateTime CreationDateUtc { get; set; }
	public uint Id => FreedesktopNotification.Id;
}

internal class Reducers
{
	public static readonly FeatureReducerCollection AllReducers = new()
	{
		FeatureReducer.Build(new DataTable<uint, NotificationState>())
			.On<AddNotificationAction>((s, a) => s.UpsertOne(new NotificationState() { FreedesktopNotification = a.Notification, CreationDateUtc = DateTime.UtcNow }))
			.On<NotificationTimerExpiredAction>((s, a) => s.Remove(a.NotificationId))
			.On<CloseNotificationAction>((s, a) => s.Remove(a.NotificationId)),
		FeatureReducer.Build(new DataTable<string, DesktopFile>())
			.On<UpdateDesktopFilesAction>((s, a) => s.UpsertMany(a.DesktopFiles)),
		FeatureReducer.Build(new AccountState())
			.On<UpdateUserAction>((s, a) => new AccountState { UserName = a.UserName, IconPath = a.IconPath }),
	};
}
