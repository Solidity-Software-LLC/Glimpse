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
				var pinnedAppToMove = s.StartMenu.PinnedLaunchers.First(f => f == a.DesktopFileKey);
				var newPinnedFiles = s.StartMenu.PinnedLaunchers.Remove(pinnedAppToMove).Insert(a.NewIndex, pinnedAppToMove);
				return s with { StartMenu = s.StartMenu with { PinnedLaunchers = newPinnedFiles } };
			})
			.On<UpdateTaskbarSlotOrderingSingleAction>((s, a) =>
			{
				var desktopFileIdMatch = s.Taskbar.PinnedLaunchers.FirstOrDefault(g => g == a.SlotRef.PinnedDesktopFileId);
				if (desktopFileIdMatch == null) return s;
				var newOrdering = s.Taskbar.PinnedLaunchers.Remove(desktopFileIdMatch).Insert(a.NewIndex, desktopFileIdMatch);
				return s with { Taskbar = s.Taskbar with { PinnedLaunchers = newOrdering } };
			}),
		FeatureReducer.Build(new DataTable<ulong, WindowProperties>())
			.On<RemoveWindowAction>((s, a) => s.Remove(a.WindowProperties))
			.On<UpdateWindowAction>((s, a) => s.UpsertOne(a.WindowProperties))
			.On<AddWindowAction>((s, a) => s.UpsertOne(a.WindowProperties)),
		FeatureReducer.Build(new DataTable<ulong, WindowProperties>())
			.On<RemoveWindowAction>((s, a) => s.Remove(a.WindowProperties))
			.On<UpdateWindowAction>((s, a) => s.UpsertOne(a.WindowProperties))
			.On<AddWindowAction>((s, a) => s.UpsertOne(a.WindowProperties)),
		FeatureReducer.Build(new DataTable<ulong, BitmapImage>())
			.On<UpdateScreenshotsAction>((s, a) => s.UpsertMany(a.Screenshots)),
		FeatureReducer.Build(new DataTable<string, Pixbuf>())
			.On<AddOrUpdateNamedIconsAction>((s, a) => s.UpsertMany(a.Icons)),
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
			.On<UpdateTaskbarSlotOrderingBulkAction>((s, a) => s with { TaskbarSlots = new SlotReferences() { Refs = a.Slots } })
			.On<UpdateTaskbarSlotOrderingSingleAction>((s, a) =>
			{
				var desktopFileIdMatch = !string.IsNullOrEmpty(a.SlotRef.PinnedDesktopFileId) ? s.TaskbarSlots.Refs.FirstOrDefault(g => g.PinnedDesktopFileId == a.SlotRef.PinnedDesktopFileId) : null;
				var classHintMatch = !string.IsNullOrEmpty(a.SlotRef.ClassHintName) ? s.TaskbarSlots.Refs.FirstOrDefault(g => g.ClassHintName == a.SlotRef.ClassHintName) : null;
				var slotToMove = desktopFileIdMatch ?? classHintMatch;
				var newOrdering = s.TaskbarSlots.Refs.Remove(slotToMove).Insert(a.NewIndex, slotToMove);
				return s with { TaskbarSlots = new SlotReferences() { Refs = newOrdering } };
			})
	};
}
