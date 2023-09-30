using System.Collections.Immutable;
using Fluxor.Selectors;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public static class TaskbarSelectors
{
	public static ISelector<TaskbarViewModel> ViewModel => SelectorFactory.CreateSelector(
		RootStateSelectors.Groups,
		RootStateSelectors.Screenshots,
		RootStateSelectors.PinnedTaskbarApps,
		(groups, screenshots, pinnedApps) =>
		{
			var allGroups = ImmutableList<TaskbarGroupViewModel>.Empty;

			foreach (var group in groups)
			{
				var desktopFile = group.DesktopFile;

				allGroups = allGroups.Add(new TaskbarGroupViewModel()
				{
					Id = group.Id,
					DesktopFile = desktopFile,
					IsPinned = pinnedApps.Any(a => a.IniFile.FilePath == group.Id),
					Tasks = group.Windows.Select(w =>
					{
						return new TaskState()
						{
							AllowedActions = w.AllowActions,
							ApplicationName = desktopFile.Name,
							DemandsAttention = w.DemandsAttention,
							DesktopFile = desktopFile,
							Icons = w.Icons,
							Title = w.Title,
							WindowRef = w.WindowRef,
							Screenshot = screenshots.FirstOrDefault(s => s.Key.Id == w.WindowRef.Id).Value ?? w.Icons?.MaxBy(i => i.Width)
						};
					}).ToImmutableList(),
					DemandsAttention = group.Windows.Any(w => w.DemandsAttention)
				});
			}

			return new TaskbarViewModel() { Groups = allGroups };
		});
}
