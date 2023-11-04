using System.Reactive.Linq;
using Fluxor;
using GLib;
using Glimpse.Services.X11;
using Glimpse.State;
using Application = Gtk.Application;

namespace Glimpse.Services.DisplayServer;

public class X11DisplayServer : IDisplayServer
{
	private readonly XLibAdaptorService _xService;

	public X11DisplayServer(XLibAdaptorService xService, IDispatcher dispatcher, Application application)
	{
		_xService = xService;

		var action = (SimpleAction) application.LookupAction("OpenStartMenu");
		StartMenuOpened = Observable.FromEventPattern(action, nameof(action.Activated)).Select(_ => true);
		FocusChanged = _xService.FocusChanged.Select(w => (IWindowRef) w);

		_xService.Windows.Subscribe(windowObs =>
		{
			windowObs.Take(1).Subscribe(w => dispatcher.Dispatch(new AddWindowAction(w)));
			windowObs.Skip(1).Subscribe(w => dispatcher.Dispatch(new UpdateWindowAction() { WindowProperties = w }));
			windowObs.TakeLast(1).Subscribe(w => dispatcher.Dispatch(new RemoveWindowAction() { WindowProperties = w }));
		});
	}

	public IObservable<bool> StartMenuOpened { get; }
	public IObservable<IWindowRef> FocusChanged { get; }

	public BitmapImage TakeScreenshot(IWindowRef windowRef)
	{
		return _xService.CaptureWindowScreenshot((XWindowRef)windowRef);
	}

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
