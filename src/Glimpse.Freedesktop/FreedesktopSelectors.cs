using System.Collections.Immutable;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Redux;
using Glimpse.Redux.Selectors;
using static Glimpse.Redux.Selectors.SelectorFactory;

namespace Glimpse.Freedesktop;

public static class FreedesktopSelectors
{
	private static readonly ISelector<AccountState> s_accountState = CreateFeatureSelector<AccountState>();
	public static readonly ISelector<string> UserIconPath = CreateSelector(s_accountState, s => s.IconPath);
}
