using Gdk;
using Gtk;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.Shared;

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
		}

		return imageBuffer?.ScaleSimple(size, size, InterpType.Bilinear);
	}
}
