using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using GtkNetPanel.Services;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;
using Microsoft.Extensions.Logging;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarController
{
	private readonly IState<RootState> _state;
	private readonly ILogger<ApplicationBarController> _logger;
	private readonly IDisplayServer _displayServer;
	private readonly FreeDesktopService _freeDesktopService;
	private ApplicationBarViewModel _currentViewModel = new();

	public ApplicationBarController(
		IState<RootState> state,
		ILogger<ApplicationBarController> logger,
		BehaviorSubject<ApplicationBarViewModel> viewModelSubject,
		IDisplayServer displayServer,
		FreeDesktopService freeDesktopService)
	{
		_state = state;
		_logger = logger;
		_displayServer = displayServer;
		_freeDesktopService = freeDesktopService;

		viewModelSubject.Subscribe(s => _currentViewModel = s);

		state.ToObservable().Select(s => s.Groups).UnbundleMany(t => t.ApplicationName).Subscribe(groupedObservable =>
		{
			groupedObservable.Select(g => g.Tasks).UnbundleMany(g => g.WindowRef.Id).Subscribe(taskObservable =>
			{
				taskObservable.DistinctUntilChanged().Subscribe(
					t => viewModelSubject.OnNext(_currentViewModel.UpdateTaskInGroup(groupedObservable.Key, t)),
					e => { },
					() => viewModelSubject.OnNext(_currentViewModel.RemoveTaskFromGroup(groupedObservable.Key, taskObservable.Key)));
			},
			e => { },
			() =>
			{
				viewModelSubject.OnNext(_currentViewModel with { Groups = _currentViewModel.Groups.Remove(groupedObservable.Key) });
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

	public void HandleDesktopFileAction(DesktopFileAction action, IconGroupViewModel group)
	{
		_freeDesktopService.Run(action);
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
