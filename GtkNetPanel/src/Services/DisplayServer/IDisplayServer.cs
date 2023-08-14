using GtkNetPanel.State;

namespace GtkNetPanel.Services.DisplayServer;

public interface IDisplayServer
{
	void ToggleWindowVisibility(GenericWindowRef stateWindowRef);
	void MakeWindowVisible(GenericWindowRef windowRef);
	BitmapImage CaptureWindowScreenshot(GenericWindowRef windowRef);
}