using System.Collections.Immutable;
using System.Reactive.Linq;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public class TaskbarSelectors
{
	public IObservable<TaskbarViewModel> ViewModel { get; }

	public TaskbarSelectors(RootStateSelectors rootStateSelectors)
	{
		ViewModel = rootStateSelectors.Groups
			.CombineLatest(rootStateSelectors.Screenshots, rootStateSelectors.PinnedTaskbarApps)
			.Select(t =>
			{
				var (groups, screenshots, pinnedApps) = t;
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
}
