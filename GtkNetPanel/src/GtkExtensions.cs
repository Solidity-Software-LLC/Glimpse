using Gdk;
using Gtk;
using Monitor = Gdk.Monitor;

namespace GtkNetPanel;

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
		widget.Children.ToList().ForEach(widget.Remove);
	}
}
