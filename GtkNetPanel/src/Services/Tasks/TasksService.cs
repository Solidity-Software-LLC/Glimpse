using Fluxor;
using GtkNetPanel.Interop.X11;
using GtkNetPanel.Services.X11;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.Tasks;

public class TasksService
{
	private readonly IDispatcher _dispatcher;
	private readonly XLibAdaptorService _xLibAdaptorService;

	public TasksService(IDispatcher dispatcher, XLibAdaptorService xLibAdaptorService)
	{
		_dispatcher = dispatcher;
		_xLibAdaptorService = xLibAdaptorService;
	}

	public void Initialize()
	{
		_xLibAdaptorService.WindowCreated.Subscribe(w =>
		{
			_dispatcher.Dispatch(new AddTaskAction() { Task = CreateTask(w) });
		});

		_xLibAdaptorService.WindowRemoved.Subscribe(w =>
		{
			_dispatcher.Dispatch(new RemoveTaskAction() { WindowId = $"{w.Display}_{w.Window}"});
		});
	}

	private TaskState CreateTask(XWindowRef windowRef)
	{
		return new TaskState()
		{
			Name = _xLibAdaptorService.GetStringProperty(windowRef, XAtoms.NetWmName),
			WindowRef = new GenericWindowRef() { Id = $"{windowRef.Display}_{windowRef.Window}", InternalRef = windowRef },
			Icons = _xLibAdaptorService.GetIcons(windowRef),
			State = _xLibAdaptorService.GetAtomArray(windowRef, XAtoms.NetWmState).ToList()
		};
	}
}
