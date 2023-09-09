using Glimpse.State;

namespace Glimpse.Services.DisplayServer;

public interface IDisplayServer
{
	void ToggleWindowVisibility(IWindowRef stateWindowRef);
	void MakeWindowVisible(IWindowRef windowRef);
	void MaximizeWindow(IWindowRef windowRef);
	void MinimizeWindow(IWindowRef windowRef);
	void StartResizing(IWindowRef windowRef);
	void StartMoving(IWindowRef windowRef);
	void CloseWindow(IWindowRef windowRef);
	BitmapImage TakeScreenshot(IWindowRef windowRef);

	IObservable<(int x, int y)> StartMenuOpen { get; }
}
