using Gdk;
using Gtk;

namespace GtkNetPanel.Components;

public static class Extensions
{
	public static void AddHoverHighlighting(this Widget widget)
	{
		widget.AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		widget.EnterNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Prelight, true);
		widget.LeaveNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Normal, true);
	}
}
