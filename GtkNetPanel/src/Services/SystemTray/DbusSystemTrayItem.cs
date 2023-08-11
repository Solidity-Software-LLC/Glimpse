using Gdk;
using Gtk;
using GtkNetPanel.Services.DBus.StatusNotifierItem;

namespace GtkNetPanel.Services.SystemTray;

public static class DbusSystemTrayItemExtensions
{
	public static Pixbuf CreateIcon(this StatusNotifierItemProperties properties, IconTheme iconTheme)
	{
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
			var colorCorrectedIconData = ImageHelper.ConvertArgbToRgba(biggestIcon.Data, biggestIcon.Width, biggestIcon.Height);
			return new Pixbuf(colorCorrectedIconData, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, 4 * biggestIcon.Width);
		}

		Console.WriteLine("System Tray - Failed to find icon for: " + properties.Title);
		return null;
	}
}
