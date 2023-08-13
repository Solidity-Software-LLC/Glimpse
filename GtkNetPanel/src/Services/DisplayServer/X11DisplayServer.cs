using GtkNetPanel.Services.X11;

namespace GtkNetPanel.Services.DisplayServer;

public class X11DisplayServer : IDisplayServer
{
	private readonly XLibAdaptorService _xService;

	public X11DisplayServer(XLibAdaptorService xService)
	{
		_xService = xService;
	}

	public void ToggleWindowVisibility(GenericWindowRef stateWindowRef)
	{
		_xService.ToggleWindowVisibility((XWindowRef)stateWindowRef.InternalRef);
	}

	public void MakeWindowVisible(GenericWindowRef windowRef)
	{
		_xService.ToggleWindowVisibility((XWindowRef)windowRef.InternalRef);
	}
}
