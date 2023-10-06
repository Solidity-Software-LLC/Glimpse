using System.Reactive.Linq;
using Gdk;
using Glimpse.Components.Taskbar;
using Glimpse.Services.DBus;
using Glimpse.State;
using Gtk;

namespace Glimpse.Extensions.Gtk;

public static class IconThemeExtensions
{
	public static IObservable<IconTheme> ObserveChange(this IconTheme iconTheme)
	{
		return Observable.FromEventPattern(iconTheme, nameof(iconTheme.Changed)).Select(_ => iconTheme);
	}

	public static Pixbuf LoadIcon(this IconTheme iconTheme, TaskState task, int size)
	{
		return iconTheme.LoadIconByName(task.DesktopFile.IconName, size)
			?? LoadIconFromTaskState(task, size)
			?? iconTheme.DefaultAppIcon(size);
	}

	public static Pixbuf LoadIcon(this IconTheme iconTheme, string iconName, int size)
	{
		return iconTheme.LoadIconByName(iconName, size) ?? iconTheme.DefaultAppIcon(size);
	}

	public static Pixbuf LoadIcon(this IconTheme iconTheme, StatusNotifierItemProperties properties)
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

	public static Pixbuf LoadIcon(this IconTheme iconTheme, TaskbarGroupViewModel group, int size)
	{
		return iconTheme.LoadIconByName(group.DesktopFile.IconName, size) ?? LoadIconFromTaskState(group.Tasks.FirstOrDefault(), size) ?? iconTheme.DefaultAppIcon(size);
	}

	private static Pixbuf LoadIconFromTaskState(TaskState task, int size)
	{
		if (task == null || task.Icons == null || !task.Icons.Any()) return null;

		var biggestIcon = task.Icons.MaxBy(i => i.Width);

		return new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width)
			.ScaleSimple(size, size, InterpType.Bilinear);
	}

	private static Pixbuf LoadIconByName(this IconTheme iconTheme, string iconName, int size)
	{
		Pixbuf imageBuffer = null;

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

	private static Pixbuf DefaultAppIcon(this IconTheme iconTheme, int size)
	{
		return iconTheme
			.LoadIcon(Stock.MissingImage, size, IconLookupFlags.DirLtr)
			.ScaleSimple(size, size, InterpType.Bilinear);
	}
}
