using System.Collections.Immutable;
using Gdk;
using Glimpse.Extensions;
using Glimpse.Extensions.Redux.Reducers;
using Glimpse.Services.Configuration;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public class AllReducers
{
	public static readonly FeatureReducerCollection Reducers = new()
	{
		FeatureReducer.Build(new ConfigurationFile())
			.On<UpdateConfigurationAction>((s, a) => a.ConfigurationFile)
			.On<ToggleTaskbarPinningAction>((s, a) => s with { Taskbar = s.Taskbar with { PinnedLaunchers = s.Taskbar.PinnedLaunchers.Toggle(a.DesktopFileId) } })
			.On<ToggleStartMenuPinningAction>((s, a) => s with { StartMenu = s.StartMenu with { PinnedLaunchers = s.StartMenu.PinnedLaunchers.Toggle(a.DesktopFileId) } })
			.On<UpdateStartMenuPinnedAppOrderingAction>((s, a) =>
			{
				if (s.StartMenu.PinnedLaunchers.SequenceEqual(a.DesktopFileKeys)) return s;
				return s with { StartMenu = s.StartMenu with { PinnedLaunchers = a.DesktopFileKeys } };
			})
			.On<UpdateTaskbarSlotOrderingBulkAction>((s, a) =>
			{
				var pinnedSlots = a.Slots.Select(r => r.PinnedDesktopFileId).Where(slot => !string.IsNullOrEmpty(slot)).ToImmutableList();
				if (pinnedSlots.SequenceEqual(s.Taskbar.PinnedLaunchers)) return s;
				return s with { Taskbar = s.Taskbar with { PinnedLaunchers = pinnedSlots } };
			}),
		FeatureReducer.Build(new DataTable<ulong, WindowProperties>())
			.On<RemoveWindowAction>((s, a) => s.Remove(a.WindowProperties))
			.On<UpdateWindowAction>((s, a) => s.UpsertOne(a.WindowProperties))
			.On<AddWindowAction>((s, a) => s.UpsertOne(a.WindowProperties)),
		FeatureReducer.Build(new DataTable<ulong, Pixbuf>())
			.On<RemoveWindowAction>((s, a) => s.Remove(a.WindowProperties.WindowRef.Id))
			.On<UpdateScreenshotsAction>((s, a) => s.UpsertMany(a.Screenshots)),
		FeatureReducer.Build(new DataTable<string, Pixbuf>())
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
		FeatureReducer.Build(new DataTable<string, DesktopFile>())
			.On<UpdateDesktopFilesAction>((s, a) => s.UpsertMany(a.DesktopFiles)),
		FeatureReducer.Build(new AccountState())
			.On<UpdateUserAction>((s, a) => new AccountState { UserName = a.UserName, IconPath = a.IconPath }),
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
		FeatureReducer.Build(new RootState())
			.On<UpdateConfigurationAction>((s, a) => s with
			{
				TaskbarSlots = new SlotReferences()
				{
					Refs = a.ConfigurationFile.Taskbar.PinnedLaunchers.Select(l => new SlotRef() { PinnedDesktopFileId = l }).ToImmutableList()
				}
			})
			.On<UpdateTaskbarSlotOrderingBulkAction>((s, a) => s with { TaskbarSlots = new SlotReferences() { Refs = a.Slots } })
	};
}
