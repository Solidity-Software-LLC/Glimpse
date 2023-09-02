namespace GtkNetPanel.Services.DisplayServer;

public interface IDisplayServer
{
	void ToggleWindowVisibility(GenericWindowRef stateWindowRef);
	void MakeWindowVisible(GenericWindowRef windowRef);
	void MaximizeWindow(GenericWindowRef windowRef);
	void MinimizeWindow(GenericWindowRef windowRef);
	void StartResizing(GenericWindowRef windowRef);
	void StartMoving(GenericWindowRef windowRef);
	void CloseWindow(GenericWindowRef windowRef);

	IObservable<(int x, int y)> StartMenuOpen { get; }
}
