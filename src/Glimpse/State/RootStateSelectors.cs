using System.Collections.Immutable;
using Gdk;
using Glimpse.Extensions.Redux.Selectors;
using Glimpse.Services.Configuration;
using Glimpse.Services.FreeDesktop;
using static Glimpse.Extensions.Redux.Selectors.SelectorFactory;

namespace Glimpse.State;

public static class RootStateSelectors
{
	public static readonly ISelector<StartMenuState> StartMenuState = CreateFeatureSelector<StartMenuState>();
	public static readonly ISelector<DataTable<string, DesktopFile>> DesktopFiles = CreateFeatureSelector<DataTable<string, DesktopFile>>();
	public static readonly ISelector<DataTable<string, Pixbuf>> NamedIcons = CreateFeatureSelector<DataTable<string, Pixbuf>>();
	public static readonly ISelector<DataTable<ulong, WindowProperties>> Windows = CreateFeatureSelector<DataTable<ulong, WindowProperties>>();
	public static readonly ISelector<DataTable<ulong, BitmapImage>> Screenshots = CreateFeatureSelector<DataTable<ulong, BitmapImage>>();
	private static readonly ISelector<AccountState> s_accountState = CreateFeatureSelector<AccountState>();
	public static readonly ISelector<string> UserIconPath = CreateSelector(s_accountState, s => s.IconPath);

	public static readonly ISelector<ConfigurationFile> Configuration = CreateFeatureSelector<ConfigurationFile>();
	public static readonly ISelector<string> VolumeCommand = CreateSelector(Configuration, s => s.VolumeCommand);
	public static readonly ISelector<string> TaskManagerCommand = CreateSelector(Configuration, s => s.TaskManagerCommand);
	public static readonly ISelector<ImmutableList<string>> TaskbarPinnedLaunchers = CreateSelector(Configuration, s => s.Taskbar.PinnedLaunchers);
	public static readonly ISelector<ImmutableList<StartMenuLaunchIconContextMenuItem>> StartMenuLaunchIconContextMenuItems = CreateSelector(Configuration, s => s.StartMenuLaunchIconContextMenu);

	private static readonly ISelector<RootState> s_rootState = CreateFeatureSelector<RootState>();
	public static readonly ISelector<SlotReferences> UserSortedSlots = CreateSelector(s_rootState, s => s.TaskbarSlots);

	public static readonly ISelector<ImmutableList<DesktopFile>> AllDesktopFiles =
		CreateSelector(DesktopFiles, s => s.ById.Values
			.OrderBy(f => f.Name)
			.Where(f => !string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Exec.FullExec))
			.ToImmutableList());
}
