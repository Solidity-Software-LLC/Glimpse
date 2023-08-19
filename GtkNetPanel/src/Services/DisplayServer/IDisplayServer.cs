using GtkNetPanel.State;

namespace GtkNetPanel.Services.DisplayServer;

public interface IDisplayServer
{
	void ToggleWindowVisibility(GenericWindowRef stateWindowRef);
	void MakeWindowVisible(GenericWindowRef windowRef);
	void CloseWindow(TaskState taskState);
	void MaximizeWindow(GenericWindowRef windowRef);
	void MinimizeWindow(GenericWindowRef windowRef);
	void StartResizing(GenericWindowRef windowRef);
	void StartMoving(GenericWindowRef windowRef);
}
