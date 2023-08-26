using Gdk;
using Gtk;
using Window = Gtk.Window;

namespace GtkNetPanel.Components.Shared;

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
		var windowY = y - window.Window.Height - 16;
		if (windowX < 8) windowX = 8;

		window.Move(windowX, windowY);
	}

	public static void CenterOnScreenAboveWidget(this Window window, Widget widget)
	{
		if (!window.Visible) return;

		var monitor = window.Display.GetMonitorAtWindow(window.Window);
		var monitorDimensions = monitor.Geometry;

		widget.Window.GetOrigin(out _, out var y);

		var windowX = monitorDimensions.Width / 2 - window.Window.Width / 2;
		var windowY = y - window.Window.Height - 16;

		window.Move(windowX, windowY);
	}

	public static void AutoPopulateGrid(this Grid grid, IEnumerable<Widget> widgets, int rowSize)
	{
		var rows = widgets.Chunk(rowSize).ToList();

		for (var i = 0; i < rows.Count; i++)
		{
			for (var j = 0; j < rows[i].Length; j++)
			{
				grid.Attach(rows[i][j], j, i, 1, 1);
			}
		}
	}
}
