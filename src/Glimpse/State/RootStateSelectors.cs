using System.Collections.Immutable;
using Fluxor.Selectors;
using Gdk;
using Glimpse.Services.Configuration;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.State;

public static class RootStateSelectors
{
	private static readonly ISelector<RootState> s_rootState = SelectorFactory.CreateFeatureSelector<RootState>();
	public static readonly ISelector<SlotReferences> TaskbarSlotCollection = SelectorFactory.CreateSelector(s_rootState, s => s.TaskbarSlots);

	public static readonly ISelector<StartMenuState> StartMenuState = SelectorFactory.CreateSelector(s_rootState, s => s.StartMenuState);

	public static readonly ISelector<ConfigurationFile> Configuration = SelectorFactory.CreateSelector(s_rootState, s => s.Configuration);
	public static readonly ISelector<string> VolumeCommand = SelectorFactory.CreateSelector(Configuration, s => s.VolumeCommand);
	public static readonly ISelector<string> TaskManagerCommand = SelectorFactory.CreateSelector(Configuration, s => s.TaskManagerCommand);
	public static readonly ISelector<ImmutableList<string>> TaskbarPinnedLaunchers = SelectorFactory.CreateSelector(Configuration, s => s.Taskbar.PinnedLaunchers);

	public static readonly ISelector<ImmutableList<StartMenuLaunchIconContextMenuItem>> StartMenuLaunchIconContextMenuItems = SelectorFactory.CreateSelector(Configuration, s => s.StartMenuLaunchIconContextMenu);

	private static readonly ISelector<AccountState> s_accountState = SelectorFactory.CreateSelector(s_rootState, s => s.AccountState);
	public static readonly ISelector<string> UserIconPath = SelectorFactory.CreateSelector(s_accountState, s => s.IconPath);

	private static readonly ISelector<Entities> s_entities = SelectorFactory.CreateSelector(s_rootState, s => s.Entities);
	public static readonly ISelector<DataTable<string, DesktopFile>> DesktopFiles = SelectorFactory.CreateSelector(s_entities, s => s.DesktopFiles);
	public static readonly ISelector<DataTable<string, Pixbuf>> NamedIcons = SelectorFactory.CreateSelector(s_entities, s => s.NamedIcons);
	public static readonly ISelector<DataTable<ulong, WindowProperties>> Windows = SelectorFactory.CreateSelector(s_entities, s => s.Windows);
	public static readonly ISelector<DataTable<ulong, BitmapImage>> Screenshots = SelectorFactory.CreateSelector(s_entities, s => s.Screenshots);

	public static readonly ISelector<ImmutableList<DesktopFile>> AllDesktopFiles =
		SelectorFactory.CreateSelector(DesktopFiles, s => s.ById.Values
			.OrderBy(f => f.Name)
			.Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec))
			.ToImmutableList());
}
