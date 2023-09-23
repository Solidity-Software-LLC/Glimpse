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

	public static string TryGetValidFilePath(this string uri)
	{
		if (string.IsNullOrEmpty(uri)) return null;
		if (!uri.EndsWith(".desktop", StringComparison.InvariantCultureIgnoreCase)) return null;
		if (!uri.StartsWith("file:///", StringComparison.InvariantCultureIgnoreCase)) return null;
		var f = uri.Replace("file:///", "");
		if (!File.Exists(f)) return null;
		return f;
	}
}
