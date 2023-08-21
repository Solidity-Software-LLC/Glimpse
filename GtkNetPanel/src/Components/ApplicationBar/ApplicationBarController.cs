using System.Collections.Immutable;
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
	private readonly IDispatcher _dispatcher;
	private readonly BehaviorSubject<ApplicationBarViewModel> _viewModelSubject;

	public ApplicationBarController(
		IState<RootState> state,
		ILogger<ApplicationBarController> logger,
		IDisplayServer displayServer,
		FreeDesktopService freeDesktopService,
		IDispatcher dispatcher)
	{
		_viewModelSubject = new BehaviorSubject<ApplicationBarViewModel>(new());

		_state = state;
		_logger = logger;
		_displayServer = displayServer;
		_freeDesktopService = freeDesktopService;
		_dispatcher = dispatcher;

		state.ToObservable().Select(s => s.Groups).DistinctUntilChanged().Subscribe(s =>
		{
			Gtk.Application.Invoke((_, _) =>
			{
				_viewModelSubject.OnNext(new ApplicationBarViewModel()
				{
					Groups = s
						.Select(g => new ApplicationBarGroupViewModel() { ApplicationName = g.ApplicationName, DesktopFile = g.DesktopFile, Tasks = g.Tasks, IsPinned = g.IsPinned })
						.ToImmutableList()
				});
			});
		});
	}

	public IObservable<ApplicationBarViewModel> ViewModel => _viewModelSubject;

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

	public void HandleWindowAction(AllowedWindowActions action, ApplicationBarGroupViewModel barGroup)
	{
		var focusedWindow = barGroup.Tasks.FirstOrDefault(t => t.WindowRef.Id == _state.Value.FocusedWindow.Id) ?? barGroup.Tasks.First();

		if (action == AllowedWindowActions.Close)
		{
			barGroup.Tasks.ForEach(t => _displayServer.CloseWindow(t));
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
