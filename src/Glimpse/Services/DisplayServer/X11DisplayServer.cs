using System.Reactive.Linq;
using Fluxor;
using Glimpse.Services.X11;
using Glimpse.State;

namespace Glimpse.Services.DisplayServer;

public class X11DisplayServer : IDisplayServer
{
	private readonly XLibAdaptorService _xService;

	public X11DisplayServer(XLibAdaptorService xService, IDispatcher dispatcher)
	{
		_xService = xService;
		StartMenuOpen = _xService.StartMenuOpen;

		_xService.Windows.Subscribe(windowObs =>
		{
			windowObs.Subscribe(w => dispatcher.Dispatch(new UpdateWindowAction() { WindowProperties = w }));
			windowObs.TakeLast(1).Subscribe(w => dispatcher.Dispatch(new RemoveWindowAction() { WindowProperties = w }));
		});
	}

	public BitmapImage TakeScreenshot(IWindowRef windowRef)
	{
		return _xService.CaptureWindowScreenshot((XWindowRef)windowRef);
	}

	public IObservable<(int x, int y)> StartMenuOpen { get; }

	public void ToggleWindowVisibility(IWindowRef windowRef)
	{
		_xService.ToggleWindowVisibility((XWindowRef)windowRef);
	}

	public void MakeWindowVisible(IWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef)windowRef);
	}

	public void CloseWindow(IWindowRef windowRef)
	{
		_xService.CloseWindow((XWindowRef)windowRef);
	}

	public void MaximizeWindow(IWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef);
		_xService.MaximizeWindow((XWindowRef) windowRef);
	}

	public void MinimizeWindow(IWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef);
		_xService.MinimizeWindow((XWindowRef) windowRef);
	}

	public void StartResizing(IWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef);
		_xService.StartResizing((XWindowRef) windowRef);
	}

	public void StartMoving(IWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef);
		_xService.StartMoving((XWindowRef) windowRef);
	}
}
