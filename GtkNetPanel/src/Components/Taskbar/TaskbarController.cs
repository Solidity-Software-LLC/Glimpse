using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using GLib;
using GtkNetPanel.Services;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;
using Microsoft.Extensions.Logging;

namespace GtkNetPanel.Components.Taskbar;

public class TaskbarController
{
	private readonly IState<RootState> _state;
	private readonly ILogger<TaskbarController> _logger;
	private readonly IDisplayServer _displayServer;
	private readonly FreeDesktopService _freeDesktopService;
	private readonly IDispatcher _dispatcher;
	private readonly BehaviorSubject<TaskbarViewModel> _viewModelSubject;

	public TaskbarController(
		IState<RootState> state,
		ILogger<TaskbarController> logger,
		IDisplayServer displayServer,
		FreeDesktopService freeDesktopService,
		IDispatcher dispatcher)
	{
		_viewModelSubject = new BehaviorSubject<TaskbarViewModel>(new());

		_state = state;
		_logger = logger;
		_displayServer = displayServer;
		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;

		state.ToObservable().Select(s => s.TaskbarGroups).DistinctUntilChanged().ObserveOn(new GLibSynchronizationContext()).Subscribe(s =>
		{
			_viewModelSubject.OnNext(new TaskbarViewModel()
			{
				Groups = s
					.Select(g => new ApplicationBarGroupViewModel() { ApplicationName = g.ApplicationName, DesktopFile = g.DesktopFile, Tasks = g.Tasks, IsPinned = g.IsPinnedToApplicationBar })
					.ToImmutableList()
			});;
		});
	}

	public IObservable<TaskbarViewModel> ViewModel => _viewModelSubject;

	public void MakeWindowVisible(GenericWindowRef windowRef)
	{
		_displayServer.MakeWindowVisible(windowRef);
	}

	public void ToggleWindowVisibility(GenericWindowRef windowRef)
	{
		_displayServer.ToggleWindowVisibility(windowRef);
	}

	public void HandleDesktopFileAction(DesktopFileAction action, ApplicationBarGroupViewModel barGroup)
	{
		_freeDesktopService.Run(action);
	}

	public void CloseWindow(GenericWindowRef windowRef)
	{
		_displayServer.CloseWindow(windowRef);
	}

	public void HandleWindowAction(AllowedWindowActions action, ApplicationBarGroupViewModel barGroup)
	{
		var focusedWindow = barGroup.Tasks.FirstOrDefault(t => t.WindowRef.Id == _state.Value.FocusedWindow.Id) ?? barGroup.Tasks.First();

		if (action == AllowedWindowActions.Close)
		{
			barGroup.Tasks.ForEach(t => _displayServer.CloseWindow(t.WindowRef));
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

	public void Launch(ApplicationBarGroupViewModel viewModel)
	{
		_freeDesktopService.Run(viewModel.DesktopFile.Exec.FullExec);
	}

	public void TogglePinning(ApplicationBarGroupViewModel viewModel)
	{
		_dispatcher.Dispatch(new TogglePinningAction() { ApplicationName = viewModel.ApplicationName });
	}
}
