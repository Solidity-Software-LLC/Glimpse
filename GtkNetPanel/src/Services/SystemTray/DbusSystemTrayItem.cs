using Gdk;
using Gtk;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.DBus.StatusNotifierItem;

public static class DbusSystemTrayItemExtensions
{
	public static Pixbuf CreateIcon(this SystemTrayItemState dbusSystemTrayItemState, IconTheme iconTheme)
	{
		var properties = dbusSystemTrayItemState.Properties;

		if (!string.IsNullOrEmpty(properties.IconThemePath))
		{
			var imageData = File.ReadAllBytes(Path.Join(properties.IconThemePath, properties.IconName) +  ".png");
			using var loader = PixbufLoader.NewWithType("png");
			loader.Write(imageData);
			loader.Close();
			return loader.Pixbuf;
		}

		if (!string.IsNullOrEmpty(properties.IconName))
		{
			return iconTheme.LoadIcon(properties.IconName, 24, IconLookupFlags.DirLtr);
		}

		if (properties.IconPixmap != null)
		{
			var biggestIcon = properties.IconPixmap.MaxBy(i => i.Width * i.Height);
			var colorCorrectedIconData = ConvertArgbToRgba(biggestIcon.Data, biggestIcon.Width, biggestIcon.Height);
			return new Pixbuf(colorCorrectedIconData, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, 4 * biggestIcon.Width);
		}

		Console.WriteLine("System Tray - Failed to find icon for: " + properties.Title);
		return null;
	}

	private static byte[] ConvertArgbToRgba(byte[] data, int width, int height)
	{
		var newArray = new byte[data.Length];
		Array.Copy(data, newArray, data.Length);

		for (var i = 0; i < 4 * width * height; i += 4)
		{
			var alpha = newArray[i];
			newArray[i] = newArray[i + 1];
			newArray[i + 1] = newArray[i + 2];
			newArray[i + 2] = newArray[i + 3];
			newArray[i + 3] = alpha;
		}

		return newArray;
	}
}
