using Gdk;
using Gtk;
using Monitor = Gdk.Monitor;

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

	public static void RemoveAllChildren(this Container widget)
	{
		var widgets = widget.Children.ToList();
		widgets.ForEach(widget.Remove);
	}

	public static Widget FindChildAtX(this Container container, int x)
	{
		for (var i=0; i<container.Children.Length; i++)
		{
			var childWidget = container.Children[i];
			childWidget.TranslateCoordinates(container, 0, 0, out var left, out _);
			var right = left + childWidget.Allocation.Width;
			if (left < 0 || x > right) continue;
			return childWidget;
		}

		return null;
	}
}
