using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using GtkNetPanel.Services;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.State;
using Microsoft.Extensions.Logging;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarController
{
	private readonly IState<TasksState> _state;
	private readonly ILogger<ApplicationBarController> _logger;
	private readonly BehaviorSubject<ApplicationBarViewModel> _viewModelSubject;
	private readonly IDisplayServer _displayServer;
	private ApplicationBarViewModel _currentViewModel = new();

	public ApplicationBarController(
		IState<TasksState> state,
		ILogger<ApplicationBarController> logger,
		BehaviorSubject<ApplicationBarViewModel> viewModelSubject,
		IDisplayServer displayServer)
	{
		_state = state;
		_logger = logger;
		_viewModelSubject = viewModelSubject;
		_displayServer = displayServer;

		viewModelSubject.Subscribe(s => _currentViewModel = s);

		state.ToObservable().Select(s => s.Tasks).Unbundle(kv => kv.Value.ApplicationName).Subscribe(groupedObservable =>
		{
			groupedObservable.UnbundleMany(t => t.Value.WindowRef.Id).Subscribe(taskObservable =>
			{
				taskObservable.Select(t => t.Value).DistinctUntilChanged().Subscribe(
					t => _viewModelSubject.OnNext(_currentViewModel.UpdateTaskInGroup(groupedObservable.Key, t)),
					e => { },
					() => _viewModelSubject.OnNext(_currentViewModel.RemoveTaskFromGroup(groupedObservable.Key, taskObservable.Key)));
			},
			e => { },
			() =>
			{
				_viewModelSubject.OnNext(_currentViewModel with { Groups = _currentViewModel.Groups.Remove(groupedObservable.Key) });
			});
		});
	}

	public void MakeWindowVisible(GenericWindowRef windowRef)
	{
		_displayServer.MakeWindowVisible(windowRef);
	}

	public void ToggleWindowVisibility(GenericWindowRef windowRef)
	{
		_displayServer.ToggleWindowVisibility(windowRef);
	}

	public void HandleWindowAction(AllowedWindowActions action, IconGroupViewModel group)
	{
		var focusedWindow = group.Tasks.FirstOrDefault(t => t.WindowRef.Id == _state.Value.FocusedWindow.Id) ?? group.Tasks.First();

		if (action == AllowedWindowActions.Close)
		{
			group.Tasks.ForEach(t => _displayServer.CloseWindow(t));
		}
		else if (action == AllowedWindowActions.Maximize)
		{
			_displayServer.MaximizeWindow(focusedWindow.WindowRef);
		}
		else if (action == AllowedWindowActions.Minimize)
		{
			_displayServer.MinimizeWindow(focusedWindow.WindowRef);
		}
		else if (action == AllowedWindowActions.Resize)
		{
			_displayServer.StartResizing(focusedWindow.WindowRef);
		}
		else if (action == AllowedWindowActions.Move)
		{
			_displayServer.StartMoving(focusedWindow.WindowRef);
		}
	}
}
