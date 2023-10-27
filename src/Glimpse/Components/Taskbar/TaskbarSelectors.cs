using System.Collections.Immutable;
using Fluxor.Selectors;
using Gdk;
using Glimpse.Extensions.Gtk;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public static class TaskbarSelectors
{
	private static ISelector<ImmutableDictionary<string, Pixbuf>> GroupIcons => SelectorFactory.CreateSelector(
		RootStateSelectors.Groups,
		RootStateSelectors.NamedIcons,
		(groups, icons) =>
		{
			return groups.ToImmutableDictionary(group => group.Id, group =>
			{
				var desktopFile = group.DesktopFile;
				var allIcons = group.Windows.SelectMany(w => w.Icons).ToList();
				var biggestIcon = allIcons.Any() ? allIcons.MaxBy(i => i.Width) : null;
				icons.TryGetValue(desktopFile.IconName, out var desktopFileIcon);
				return desktopFileIcon ?? biggestIcon?.ToPixbuf() ?? Assets.MissingImage;
			});
		});


	private static ISelector<ImmutableDictionary<ulong, TaskState>> Tasks => SelectorFactory.CreateSelector(
		RootStateSelectors.Screenshots,
		RootStateSelectors.Windows,
		(screenshots, windows) =>
		{
			return windows.Values.Select(w => new TaskState()
			{
				Icon = w.Icons.Any() ? w.Icons.MaxBy(i => i.Width)?.ToPixbuf() : Assets.MissingImage,
				Title = w.Title,
				WindowRef = w.WindowRef,
				AllowedActions = w.AllowActions,
				Screenshot = screenshots.FirstOrDefault(s => s.Key.Id == w.WindowRef.Id).Value?.ToPixbuf()
					?? w.Icons?.MaxBy(i => i.Width)?.ToPixbuf()
					?? Assets.MissingImage
			}).ToImmutableDictionary(t => t.WindowRef.Id, t => t);
		});

	private static ISelector<ImmutableDictionary<string, TaskbarGroupContextMenuViewModel>> ContextMenus => SelectorFactory.CreateSelector(
		RootStateSelectors.Groups,
		GroupIcons,
		RootStateSelectors.PinnedTaskbarApps,
		RootStateSelectors.NamedIcons,
		(groups, groupIcons, pinnedApps, icons) =>
		{
			return groups.ToImmutableDictionary(group => group.Id, group => new TaskbarGroupContextMenuViewModel()
			{
				IsPinned = pinnedApps.Any(a => a.IniFile.FilePath == group.Id),
				DesktopFile = group.DesktopFile,
				LaunchIcon = groupIcons[group.Id],
				CanClose = group.Windows.Any(), // Check window actions.  Careful not to cause it to run a bunch here
				ActionIcons = group.DesktopFile.Actions.ToDictionary(
					a => a.ActionName,
					a => string.IsNullOrEmpty(a.IconName) ? groupIcons[group.Id]
						: icons.TryGetValue(a.IconName, out var i) ? i
						: Assets.MissingImage)

			});
		});

	public static ISelector<TaskbarViewModel> ViewModel => SelectorFactory.CreateSelector(
		RootStateSelectors.Groups,
		Tasks,
		GroupIcons,
		ContextMenus,
		(groups, tasks, groupIcons, contextMenus) =>
			new TaskbarViewModel()
			{
				Groups = groups
					.Select(g => new TaskbarGroupViewModel()
					{
						Id = g.Id,
						DesktopFile = g.DesktopFile,
						Icon = groupIcons[g.Id],
						Tasks = g.Windows.Select(w => tasks[w.WindowRef.Id]).ToImmutableList(),
						DemandsAttention = g.Windows.Any(w => w.DemandsAttention),
						ContextMenu = contextMenus[g.Id]
					})
					.ToImmutableList()
			});
}
