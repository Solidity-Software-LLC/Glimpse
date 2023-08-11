using System.Text;
using Gdk;
using Gtk;
using Monitor = Gdk.Monitor;

namespace GtkNetPanel.Services.GtkSharp;

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

	public static string GetStringProperty(this Gdk.Window window, Atom property)
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

	public static Atom[] GetAtomProperty(this Gdk.Window window, Atom property)
	{
		var success = Property.Get(window, property, false, out Atom[] data);
		return success ? data : null;
	}

	public static List<WindowIcon> GetIcons(this Gdk.Window window, Atom property)
	{
		var success = Property.Get(window, property, Atoms.AnyPropertyType, 0, 1024 * 1024 * 5, 0, out _, out _, out _, out var data);
		if (!success) return null;

		var icons = new List<WindowIcon>();
		var binaryReader = new BinaryReader(new MemoryStream(data));

		while (binaryReader.PeekChar() != -1)
		{
			var width = binaryReader.ReadInt64();
			var height = binaryReader.ReadInt64();
			var numPixels = width * height;
			var imageData = new byte[numPixels * sizeof(int)];

			for (var i = 0; i < numPixels * sizeof(int); i += sizeof(int))
			{
				var intBytes = BitConverter.GetBytes(binaryReader.ReadInt32());
				binaryReader.ReadInt32();
				imageData[i] = intBytes[3];
				imageData[i+1] = intBytes[2];
				imageData[i+2] = intBytes[1];
				imageData[i+3] = intBytes[0];
			}

			icons.Add(new WindowIcon()
			{
				Width = (int) width,
				Height = (int) height,
				Data = imageData
			});
		}
		return icons;
	}
}

public class WindowIcon
{
	public int Width { get; set; }
	public int Height { get; set; }
	public byte[] Data { get; set; }
}
