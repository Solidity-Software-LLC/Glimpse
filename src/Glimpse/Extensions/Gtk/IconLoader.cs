using Gdk;
using Glimpse.Services.DBus;
using Glimpse.State;
using Gtk;

namespace Glimpse.Extensions.Gtk;

public static class IconLoader
{
	public static Pixbuf DefaultAppIcon(int size)
	{
		return IconTheme.Default
			.LoadIcon("application-default-icon", size, IconLookupFlags.DirLtr)
			.ScaleSimple(size, size, InterpType.Bilinear);
	}

	public static Pixbuf LoadIcon(TaskState task, int size)
	{
		if (task == null) return null;

		var biggestIcon = task.Icons.MaxBy(i => i.Width);

		return new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width)
			.ScaleSimple(size, size, InterpType.Bilinear);
	}

	public static Pixbuf LoadIcon(string iconName, int size)
	{
		Pixbuf imageBuffer = null;
		var iconTheme = IconTheme.Default;

		if (!string.IsNullOrEmpty(iconName))
		{
			if (iconName.StartsWith("/"))
			{
				imageBuffer = new Pixbuf(File.ReadAllBytes(iconName));
			}
			else if (iconTheme.HasIcon(iconName))
			{
				imageBuffer = iconTheme.LoadIcon(iconName, size, IconLookupFlags.DirLtr);
			}
			else if (File.Exists($"/usr/share/pixmaps/{iconName}.png"))
			{
				imageBuffer = new Pixbuf(File.ReadAllBytes($"/usr/share/pixmaps/{iconName}.png"));
			}
		}

		return imageBuffer?.ScaleSimple(size, size, InterpType.Bilinear);
	}

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
