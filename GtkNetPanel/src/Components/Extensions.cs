using Gdk;
using Gtk;
using Window = Gtk.Window;

namespace GtkNetPanel.Components;

public static class Extensions
{
	public static void AddHoverHighlighting(this Widget widget)
	{
		widget.AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		widget.EnterNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Prelight, true);
		widget.LeaveNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Normal, true);
	}

	public static void CenterAbove(this Window window, Widget widget)
	{
		if (!window.Visible) return;

		widget.Window.GetOrigin(out _, out var y);
		widget.TranslateCoordinates(widget.Toplevel, 0, 0, out var x, out _);

		var windowX = x + widget.Window.Width / 2 - window.Window.Width / 2;
		var windowY = y - window.Window.Height - 8;
		if (windowX < 8) windowX = 8;

		window.Move(windowX, windowY);
	}
}
