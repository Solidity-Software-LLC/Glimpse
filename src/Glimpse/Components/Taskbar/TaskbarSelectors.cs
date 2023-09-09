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
				var allGroups = ImmutableList<TaskbarGroupViewModel>.Empty;

				foreach (var desktopFile in pinnedApps)
				{
					allGroups = allGroups.Add(new TaskbarGroupViewModel()
					{
						Id = desktopFile.IniFile.FilePath,
						DesktopFile = desktopFile,
						IsPinned = true
					});
				}

				foreach (var task in tasks)
				{
					var desktopFile = task.DesktopFile;
					var matchingGroup = allGroups.FirstOrDefault(g => g.DesktopFile.IniFile.FilePath == task.DesktopFile.IniFile.FilePath);

					if (matchingGroup == null)
					{
						allGroups = allGroups.Add(new TaskbarGroupViewModel()
						{
							Id = task.DesktopFile.IniFile.FilePath,
							DesktopFile = desktopFile,
							IsPinned = false,
							Tasks = ImmutableList<TaskState>.Empty.Add(task),
							DemandsAttention = task.DemandsAttention
						});
					}
					else
					{
						allGroups = allGroups.Replace(matchingGroup, matchingGroup with { Tasks = matchingGroup.Tasks.Add(task), DemandsAttention = matchingGroup.DemandsAttention || task.DemandsAttention });
					}
				}

				return new TaskbarViewModel() { Groups = allGroups };
			});
	}
}
