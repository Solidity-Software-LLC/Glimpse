using System.Collections.Immutable;
using System.Reactive.Linq;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public class TaskbarSelectors
{
	public IObservable<TaskbarViewModel> ViewModel { get; }

	public TaskbarSelectors(RootStateSelectors rootStateSelectors)
	{
		ViewModel = rootStateSelectors.Tasks
			.CombineLatest(rootStateSelectors.PinnedTaskbarApps)
			.Select(t =>
			{
				var (tasks, pinnedApps) = t;
				var allGroups = ImmutableList<ApplicationBarGroupViewModel>.Empty;

				foreach (var pinned in pinnedApps)
				{
					allGroups = allGroups.Add(new ApplicationBarGroupViewModel()
					{
						ApplicationName = pinned.Name,
						DesktopFile = pinned,
						IsPinned = true
					});
				}

				foreach (var task in tasks)
				{
					var desktopFile = task.DesktopFile;
					var matchingGroup = allGroups.FirstOrDefault(g => g.DesktopFile.IniFile.FilePath == task.DesktopFile.IniFile.FilePath);

					if (matchingGroup == null)
					{
						allGroups = allGroups.Add(new ApplicationBarGroupViewModel()
						{
							ApplicationName = desktopFile.Name,
							DesktopFile = desktopFile,
							IsPinned = false,
							Tasks = ImmutableList<TaskState>.Empty.Add(task)
						});
					}
					else
					{
						matchingGroup.Tasks = matchingGroup.Tasks.Add(task);
					}
				}

				return new TaskbarViewModel() { Groups = allGroups };
			});
	}
}
