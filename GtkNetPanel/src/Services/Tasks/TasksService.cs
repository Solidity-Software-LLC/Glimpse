using Fluxor;
using Gdk;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.Tasks;

public class TasksService
{
	private readonly IDispatcher _dispatcher;

	public TasksService(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}

	public void Initialize()
	{
		foreach (var window in Display.Open(null).DefaultScreen.WindowStack.Where(w => w.TypeHint == WindowTypeHint.Normal))
		{
			var task = CreateTask(window);
			_dispatcher.Dispatch(new AddTaskAction() { Task = task });
		}
	}

	private TaskState CreateTask(Window window)
	{
		var state = window.GetAtomProperty(Atoms._NET_WM_STATE);
		var icons = window.GetIcons(Atoms._NET_WM_ICON);
		var name = window.GetStringProperty(Atoms._NET_WM_NAME);
		var processId = window.GetIntProperty(Atoms._NET_WM_PID);
		return new TaskState() { ProcessId = processId, Name = name, Icons = icons, State = state.ToList() };
	}
}
