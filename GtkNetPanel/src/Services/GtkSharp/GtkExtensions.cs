using System.Text;
using Gdk;
using Gtk;
using Monitor = Gdk.Monitor;
using Window = Gdk.Window;

namespace GtkNetPanel.Services.GtkSharp;

public static class GtkExtensions
{
	public static Pixbuf GetWindowScreenshot(this Window window, int targetHeight)
	{
		var ratio = (double) window.Width / window.Height;
		return new Pixbuf(window, 0, 0, window.Width, window.Height).ScaleSimple((int) (targetHeight * ratio), targetHeight, InterpType.Bilinear);
	}

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

	public static string GetStringProperty(this Window window, Atom property)
	{
		var success = Property.Get(window, property, Atoms.AnyPropertyType, 0, 1024, 0, out var actualReturnType, out _, out _, out var data);

		if (!success) return null;

		if (actualReturnType.Name == Atoms.STRING.Name || actualReturnType.Name == Atoms.UTF8_STRING.Name)
		{
			return Encoding.UTF8.GetString(data);
		}

		if (actualReturnType.Name == Atoms.COMPOUND_TEXT)
		{
			return Encoding.UTF8.GetString(data);
		}

		return null;
	}

	public static Atom[] GetAtomProperty(this Window window, Atom property)
	{
		var success = Property.Get(window, property, false, out Atom[] data);
		return success ? data : null;
	}

	public static int GetIntProperty(this Window window, Atom property)
	{
		var success = Property.Get(window, property, false, out int[] data);
		return success ? data[0] : 0;
	}
}
