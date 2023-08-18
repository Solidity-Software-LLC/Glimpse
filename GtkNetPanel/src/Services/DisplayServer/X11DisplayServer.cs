using Fluxor;
using GtkNetPanel.Interop.X11;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.Services.X11;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.DisplayServer;

public class X11DisplayServer : IDisplayServer
{
	private readonly XLibAdaptorService _xService;
	private readonly IDispatcher _dispatcher;
	private readonly FreeDesktopService _freeDesktopService;

	public X11DisplayServer(XLibAdaptorService xService, IDispatcher dispatcher, FreeDesktopService freeDesktopService)
	{
		_xService = xService;
		_dispatcher = dispatcher;
		_freeDesktopService = freeDesktopService;
	}

	public BitmapImage CaptureWindowScreenshot(GenericWindowRef windowRef)
	{
		return _xService.CaptureWindowScreenshot((XWindowRef)windowRef.InternalRef);
	}

	public void ToggleWindowVisibility(GenericWindowRef windowRef)
	{
		_xService.ToggleWindowVisibility((XWindowRef)windowRef.InternalRef);
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
			Title = _xService.GetStringProperty(windowRef, XAtoms.NetWmName),
			WindowRef = new GenericWindowRef() { Id = $"{windowRef.Display}_{windowRef.Window}", InternalRef = windowRef },
			Icons = _xService.GetIcons(windowRef),
			State = _xService.GetAtomArray(windowRef, XAtoms.NetWmState).ToList(),
			ApplicationName = _xService.GetClassHint(windowRef).res_name,
			DesktopFile = _freeDesktopService.FindAppDesktopFile(_xService.GetClassHint(windowRef).res_name)
		};
	}
}
