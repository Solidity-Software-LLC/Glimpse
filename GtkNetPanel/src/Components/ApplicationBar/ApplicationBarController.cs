using System.Reactive.Subjects;
using Fluxor;
using Gtk;
using GtkNetPanel.Services;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.State;
using Microsoft.Extensions.Logging;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarController
{
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
		_logger = logger;
		_viewModelSubject = viewModelSubject;
		_displayServer = displayServer;

		state.ToObservable().Subscribe(s =>
		{
			_currentViewModel = new ApplicationBarViewModel() { Tasks = s.Tasks, ShownWindowPicker = _currentViewModel.ShownWindowPicker };
			_viewModelSubject.OnNext(_currentViewModel);
		});
	}

	public BitmapImage CaptureWindowScreenshot(GenericWindowRef windowRef)
	{
		return _displayServer.CaptureWindowScreenshot(windowRef);
	}

	public void OnPreviewWindowClicked(ButtonReleaseEventArgs e, GenericWindowRef windowRef)
	{
		_displayServer.MakeWindowVisible(windowRef);
	}

	public void OnClickApplicationIcon(ButtonReleaseEventArgs e, TaskState state)
	{
		if (e.Event.Button == 1)
		{
			_displayServer.ToggleWindowVisibility(state.WindowRef);
		}
		else
		{
			_currentViewModel = new ApplicationBarViewModel() { Tasks = _currentViewModel.Tasks, ShownWindowPicker = state };
			_viewModelSubject.OnNext(_currentViewModel);
		}
	}
}
