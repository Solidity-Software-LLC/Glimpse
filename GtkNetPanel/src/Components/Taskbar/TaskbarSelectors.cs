using System.Reactive.Linq;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.Taskbar;

public class TaskbarSelectors
{
	public IObservable<TaskbarViewModel> ViewModel { get; }

	public TaskbarSelectors(RootStateSelectors rootStateSelectors)
	{
		var groupsObservable = rootStateSelectors
			.TaskbarState.Select(s => s.Groups)
			.DistinctUntilChanged();

		ViewModel = groupsObservable
			.CombineLatest(rootStateSelectors.PinnedTaskbarApps)
			.Select(t =>
			{
				var groups = t.First;
				var pinnedApps = t.Second;
				var viewModel = new TaskbarViewModel();

				foreach (var g in groups)
				{
					var groupViewModel = new ApplicationBarGroupViewModel()
					{
						ApplicationName = g.ApplicationName,
						DesktopFile = g.DesktopFile,
						Tasks = g.Tasks,
						IsPinned = pinnedApps.Any(f => f.IniConfiguration.FilePath == g.DesktopFile.IniConfiguration.FilePath)
					};

					viewModel.Groups = viewModel.Groups.Add(groupViewModel);
				}

				return viewModel;
			});
	}
}
