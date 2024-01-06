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

internal class Reducers
{
	public static readonly FeatureReducerCollection AllReducers =
	[
		FeatureReducer.Build(new DataTable<string, DesktopFile>())
			.On<UpdateDesktopFilesAction>((s, a) => s.UpsertMany(a.DesktopFiles)),
		FeatureReducer.Build(new AccountState())
			.On<UpdateUserAction>((s, a) => new AccountState { UserName = a.UserName, IconPath = a.IconPath })
	];
}
