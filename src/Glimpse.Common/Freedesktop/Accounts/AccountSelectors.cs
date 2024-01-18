using Glimpse.Redux.Selectors;
using static Glimpse.Redux.Selectors.SelectorFactory;

namespace Glimpse.Freedesktop;

public static class AccountSelectors
{
	private static readonly ISelector<AccountState> s_accountState = CreateFeatureSelector<AccountState>();
	public static readonly ISelector<string> UserIconPath = CreateSelector(s_accountState, s => s.IconPath);
}
