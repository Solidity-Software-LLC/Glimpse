using Glimpse.Redux.Selectors;
using static Glimpse.Redux.Selectors.SelectorFactory;

namespace Glimpse.UI.State;

public static class UISelectors
{
	public static readonly ISelector<StartMenuState> StartMenuState = CreateFeatureSelector<StartMenuState>();
	public static readonly ISelector<UIState> Root = CreateFeatureSelector<UIState>();
	public static readonly ISelector<SlotReferences> UserSortedSlots = CreateSelector(Root, s => s.TaskbarSlots);


}
