using System.Reactive.Linq;
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

		state.ToObservable().SelectMany(s => s.Tasks).GroupBy(s => s.Value.ApplicationName).Subscribe(s =>
		{
			s.Take(1).Select(kv => kv.Value).Subscribe(task =>
			{
				_currentViewModel = _currentViewModel with { Groups = _currentViewModel.Groups.Add(s.Key, new IconGroupViewModel(task)) };
				_viewModelSubject.OnNext(_currentViewModel);
			});

			s.Skip(1).Select(kv => kv.Value).Subscribe(task =>
			{
				_currentViewModel = _currentViewModel with { Groups = _currentViewModel.Groups.SetItem(s.Key, _currentViewModel.Groups[s.Key].UpdateTask(task)) };
				_viewModelSubject.OnNext(_currentViewModel);
			},
			e => { },
			() =>
			{
				_currentViewModel = _currentViewModel with { Groups = _currentViewModel.Groups.Remove(s.Key) };
				_viewModelSubject.OnNext(_currentViewModel);
			});
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

	public void OnClickApplicationIcon(ButtonReleaseEventArgs e, string applicationName)
	{
		var group = _currentViewModel.Groups[applicationName];

		if (e.Event.Button == 1 && group.Tasks.Count == 1)
		{
			_displayServer.ToggleWindowVisibility(group.Tasks.First().WindowRef);
		}
		else
		{
			_currentViewModel = _currentViewModel with { GroupForWindowPicker = applicationName };
			_viewModelSubject.OnNext(_currentViewModel);
		}
	}

	public void CloseWindowPicker()
	{
		_currentViewModel = _currentViewModel with { GroupForWindowPicker = null };
		_viewModelSubject.OnNext(_currentViewModel);
	}
}
