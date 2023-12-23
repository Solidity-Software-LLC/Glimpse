using Glimpse.Images;
using Glimpse.Redux;
using Glimpse.Redux.Reducers;

namespace Glimpse.UI.State;

internal class UIReducers
{
	public static readonly FeatureReducerCollection AllReducers = new()
	{
		FeatureReducer.Build(new DataTable<string, IGlimpseImage>())
			.On<AddOrUpdateNamedIconsAction>((s, a) =>
			{
				var result = s;

				foreach (var kv in a.Icons)
				{
					if (result.ContainsKey(kv.Key) && kv.Value == null)
					{
						result = result.Remove(kv.Key);
					}
					else if (kv.Value != null)
					{
						result = result.UpsertOne(kv.Key, kv.Value);
					}
				}

				return result;
			}),
		FeatureReducer.Build(new StartMenuState())
			.On<UpdateAppFilteringChip>((s, a) =>
			{
				var chips = s.Chips;
				chips = chips.SetItem(StartMenuChips.Pinned, chips[StartMenuChips.Pinned] with { IsSelected = a.Chip == StartMenuChips.Pinned });
				chips = chips.SetItem(StartMenuChips.AllApps, chips[StartMenuChips.AllApps] with { IsSelected = a.Chip == StartMenuChips.AllApps });
				chips = chips.SetItem(StartMenuChips.SearchResults, chips[StartMenuChips.SearchResults] with { IsSelected = a.Chip == StartMenuChips.SearchResults });
				return s with { Chips = chips };
			})
			.On<UpdateStartMenuSearchTextAction>((s, a) =>
			{
				var chips = s.Chips;

				if (string.IsNullOrEmpty(a.SearchText))
				{
					chips = chips.SetItem(StartMenuChips.Pinned, new StartMenuAppFilteringChip { IsSelected = true, IsVisible = true });
					chips = chips.SetItem(StartMenuChips.AllApps, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = true });
					chips = chips.SetItem(StartMenuChips.SearchResults, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = false });
				}
				else
				{
					chips = chips.SetItem(StartMenuChips.Pinned, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = true });
					chips = chips.SetItem(StartMenuChips.AllApps, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = true });
					chips = chips.SetItem(StartMenuChips.SearchResults, new StartMenuAppFilteringChip { IsSelected = true, IsVisible = true });
				}

				return s with { SearchText = a.SearchText, Chips = chips };
			}),
		FeatureReducer.Build(new UIState())
			.On<UpdateTaskbarSlotOrderingBulkAction>((s, a) => s with { TaskbarSlots = new SlotReferences() { Refs = a.Slots } })
	};
}
