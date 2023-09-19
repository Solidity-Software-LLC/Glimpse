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

	public static int ReplaceChild(this FlowBox box, FlowBoxChild oldWidget, FlowBoxChild newWidget)
	{
		var index = Array.FindIndex(box.Children, c => c == oldWidget);
		var newWidgetIndex = Array.FindIndex(box.Children, c => c == newWidget);

		if (newWidgetIndex != -1)
		{
			box.Remove(newWidget);
		}

		box.Insert(newWidget, index);
		box.Remove(oldWidget);
		return index;
	}
}
