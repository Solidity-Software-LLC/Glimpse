using Gtk;

namespace GtkNetPanel.Components.Shared.ContextMenu;

public class ContextMenuEventArgs : EventArgs
{
	public ContextMenuEventArgs(Widget widget, bool rightClick)
	{
		Widget = widget;
		RightClick = rightClick;
	}

	public Widget Widget { get; }

	public bool RightClick { get; }
}
