using System.Reactive.Subjects;
using Fluxor;
using Gdk;
using Gtk;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;
using Microsoft.Extensions.Logging;
using Window = Gdk.Window;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarController
{
	private readonly ILogger<ApplicationBarController> _logger;
	private readonly BehaviorSubject<ApplicationBarViewModel> _viewModelSubject;
	private ApplicationBarViewModel _currentViewModel = new();

	public ApplicationBarController(IState<TasksState> state, ILogger<ApplicationBarController> logger, BehaviorSubject<ApplicationBarViewModel> viewModelSubject)
	{
		_logger = logger;
		_viewModelSubject = viewModelSubject;

		state.ToObservable().Subscribe(s =>
		{
			_currentViewModel = new ApplicationBarViewModel() { Tasks = s.Tasks, ShownWindowPicker = _currentViewModel.ShownWindowPicker };
			_viewModelSubject.OnNext(_currentViewModel);
		});
	}

	public void OnClickApplicationIcon(ButtonReleaseEventArgs e, TaskState state)
	{
		var matchingWindow = Display.Default.DefaultScreen.WindowStack.FirstOrDefault(w => w.GetIntProperty(Atoms._NET_WM_PID) == state.ProcessId);

		if (matchingWindow == null)
		{
			_logger.LogWarning($"Failed to find window for process {state.ProcessId}");
		}
		else if (e.Event.Button == 1)
		{
			ToggleWindowVisibility(matchingWindow);
		}
		else
		{
			_currentViewModel = new ApplicationBarViewModel() { Tasks = _currentViewModel.Tasks, ShownWindowPicker = state };
			_viewModelSubject.OnNext(_currentViewModel);
		}
	}

	private void ToggleWindowVisibility(Window window)
	{
		var states = window.GetAtomProperty(Atom.Intern("_NET_WM_STATE", true));

		if (states.Any(a => a.Name == "_NET_WM_STATE_HIDDEN"))
		{
			window.Raise();
		}
		else if (states.All(a => a.Name != "_NET_WM_STATE_FOCUSED"))
		{
			window.Focus(0);
		}
		else
		{
			window.Iconify();
		}
	}
}
