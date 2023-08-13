using Fluxor;
using GtkNetPanel.Interop.X11;
using GtkNetPanel.Services.X11;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.DisplayServer;

public class X11DisplayServer : IDisplayServer
{
	private readonly XLibAdaptorService _xService;
	private readonly IDispatcher _dispatcher;

	public X11DisplayServer(XLibAdaptorService xService, IDispatcher dispatcher)
	{
		_xService = xService;
		_dispatcher = dispatcher;
	}

	public void ToggleWindowVisibility(GenericWindowRef stateWindowRef)
	{
		_xService.ToggleWindowVisibility((XWindowRef)stateWindowRef.InternalRef);
	}

	public void MakeWindowVisible(GenericWindowRef windowRef)
	{
		_xService.ToggleWindowVisibility((XWindowRef)windowRef.InternalRef);
	}

	public void Initialize()
	{
		_xService.WindowCreated.Subscribe(w =>
		{
			_dispatcher.Dispatch(new AddTaskAction() { Task = CreateTask(w) });
		});

		_xService.WindowRemoved.Subscribe(w =>
		{
			_dispatcher.Dispatch(new RemoveTaskAction() { WindowId = $"{w.Display}_{w.Window}"});
		});
	}

	private TaskState CreateTask(XWindowRef windowRef)
	{
		return new TaskState()
		{
			Name = _xService.GetStringProperty(windowRef, XAtoms.NetWmName),
			WindowRef = new GenericWindowRef() { Id = $"{windowRef.Display}_{windowRef.Window}", InternalRef = windowRef },
			Icons = _xService.GetIcons(windowRef),
			State = _xService.GetAtomArray(windowRef, XAtoms.NetWmState).ToList()
		};
	}
}
