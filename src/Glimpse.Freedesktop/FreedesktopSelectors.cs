using System.Collections.Immutable;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Redux;
using Glimpse.Redux.Selectors;
using static Glimpse.Redux.Selectors.SelectorFactory;

namespace Glimpse.Freedesktop;

public static class FreedesktopSelectors
{
	public static readonly ISelector<DataTable<string, DesktopFile>> DesktopFiles = CreateFeatureSelector<DataTable<string, DesktopFile>>();
	private static readonly ISelector<AccountState> s_accountState = CreateFeatureSelector<AccountState>();
	public static readonly ISelector<string> UserIconPath = CreateSelector(s_accountState, s => s.IconPath);
	public static readonly ISelector<ImmutableList<DesktopFile>> AllDesktopFiles =
		CreateSelector(DesktopFiles, s => s.ById.Values
			.OrderBy(f => f.Name)
			.ToImmutableList());
}
