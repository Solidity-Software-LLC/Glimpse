using Gdk;
using Gtk;
using Monitor = Gdk.Monitor;
using Window = Gdk.Window;

namespace Glimpse.Extensions.Gtk;

public static class GtkExtensions
{
	public static IEnumerable<Monitor> GetMonitors(this Display display)
	{
		for (var i = 0; i < display.NMonitors; i++)
		{
			yield return display.GetMonitor(i);
		}
	}

	public static bool Contains(this Monitor monitor, Window window)
	{
		window.GetRootCoords(0, 0, out var x, out var y);
		return monitor.Geometry.Contains(x, y);
	}

	public static void RemoveAllChildren(this Container widget)
	{
		var widgets = widget.Children.ToList();
		widgets.ForEach(w => w.Destroy());
	}

	public static bool ContainsPoint(this Widget widget, int px, int py)
	{
		if (!widget.IsVisible) return false;
		widget.Window.GetGeometry(out var x, out var y, out var width, out var height);
		return px >= x && py >= y && px < x + width && py < y + height;
	}

	public static bool IsPointerInside(this Widget widget)
	{
		widget.Display.GetPointer(out var px, out var py);
		return widget.ContainsPoint(px, py);
	}
}
