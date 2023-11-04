using System.Collections.Immutable;
using Fluxor;
using Gdk;
using Glimpse.Extensions;
using Glimpse.Services.Configuration;
using Glimpse.Services.FreeDesktop;
using Glimpse.State.StateLens;
using static Glimpse.State.StateLens.Reducers;

namespace Glimpse.State;

public class AllReducers : IReducer<RootState>
{
	private static readonly List<On<RootState>> s_reducers =
		CombineReducers(
			CreateSubReducers<RootState, RootState>(s => s, (s, t) => t)
				.On<UpdateConfigurationAction>((s, a) => s with { Configuration = a.ConfigurationFile })
				.On<UpdateTaskbarSlotOrderingBulkAction>((s, a) => s with { TaskbarSlots = new SlotReferences() { Refs = a.Slots } })
				.On<UpdateTaskbarSlotOrderingSingleAction>((s, a) =>
				{
					var desktopFileIdMatch = !string.IsNullOrEmpty(a.SlotRef.DesktopFileId) ? s.TaskbarSlots.Refs.FirstOrDefault(g => g.DesktopFileId == a.SlotRef.DesktopFileId) : null;
					var classHintMatch = !string.IsNullOrEmpty(a.SlotRef.ClassHintName) ? s.TaskbarSlots.Refs.FirstOrDefault(g => g.ClassHintName == a.SlotRef.ClassHintName) : null;
					var slotToMove = desktopFileIdMatch ?? classHintMatch;
					var newOrdering = s.TaskbarSlots.Refs.Remove(slotToMove).Insert(a.NewIndex, slotToMove);
					var pinnedDesktopFiles = newOrdering.Select(g => g.DesktopFileId).Where(f => !string.IsNullOrEmpty(f)).ToImmutableList();
					var newState = s with { TaskbarSlots = new SlotReferences() { Refs = newOrdering } };

					if (!pinnedDesktopFiles.SequenceEqual(s.Configuration.Taskbar.PinnedLaunchers))
					{
						newState = newState with { Configuration = newState.Configuration with { Taskbar = s.Configuration.Taskbar with { PinnedLaunchers = pinnedDesktopFiles } } };
					}

					return newState;
				}),
			CreateSubReducers<RootState, TaskbarConfiguration>(s => s.Configuration.Taskbar, (s, t) => s with { Configuration = s.Configuration with { Taskbar = t } })
				.On<ToggleTaskbarPinningAction>((s, a) => s with { PinnedLaunchers = s.PinnedLaunchers.Toggle(a.DesktopFileId) }),
			CreateSubReducers<RootState, StartMenuConfiguration>(s => s.Configuration.StartMenu, (s, t) => s with { Configuration = s.Configuration with { StartMenu = t } })
				.On<ToggleStartMenuPinningAction>((s, a) => s with { PinnedLaunchers = s.PinnedLaunchers.Toggle(a.DesktopFileId) })
				.On<UpdateStartMenuPinnedAppOrderingAction>((s, a) =>
				{
					var pinnedAppToMove = s.PinnedLaunchers.First(f => f == a.DesktopFileKey);
					var newPinnedFiles = s.PinnedLaunchers.Remove(pinnedAppToMove).Insert(a.NewIndex, pinnedAppToMove);
					return s with { PinnedLaunchers = newPinnedFiles };
				}),
			CreateSubReducers<RootState, DataTable<ulong, WindowProperties>>(s => s.Entities.Windows, (s, windows) => s with { Entities = s.Entities with { Windows = windows } })
				.On<RemoveWindowAction>((s, a) => s.Remove(a.WindowProperties))
				.On<UpdateWindowAction>((s, a) => s.UpsertOne(a.WindowProperties))
				.On<AddWindowAction>((s, a) => s.UpsertOne(a.WindowProperties)),
			CreateSubReducers<RootState, DataTable<ulong, BitmapImage>>(s => s.Entities.Screenshots, (s, t) => s with { Entities = s.Entities with { Screenshots = t } })
				.On<UpdateScreenshotsAction>((s, a) => s.UpsertMany(a.Screenshots)),
			CreateSubReducers<RootState, DataTable<string, Pixbuf>>(s => s.Entities.NamedIcons, (s, t) => s with { Entities = s.Entities with { NamedIcons = t } })
				.On<AddOrUpdateNamedIconsAction>((s, a) => s.UpsertMany(a.Icons)),
			CreateSubReducers<RootState, DataTable<string, DesktopFile>>(s => s.Entities.DesktopFiles, (s, t) => s with { Entities = s.Entities with { DesktopFiles = t } })
				.On<UpdateDesktopFilesAction>((s, a) => s.UpsertMany(a.DesktopFiles)),
			CreateSubReducers<RootState, AccountState>(s => s.AccountState, (s, t) => s with { AccountState = t })
				.On<UpdateUserAction>((s, a) => new AccountState { UserName = a.UserName, IconPath = a.IconPath }),
			CreateSubReducers<RootState, StartMenuState>(s => s.StartMenuState, (s, t) => s with { StartMenuState = t })
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
				})

				.ToList());

	public RootState Reduce(RootState state, object action)
	{
		return s_reducers.Aggregate(state, (s, on) => on.Reduce(s, action));
	}

	public bool ShouldReduceStateForAction(object action) => true;
}
