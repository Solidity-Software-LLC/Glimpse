using System.Reactive.Linq;
using Gdk;
using Gtk;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.Extensions.Gtk;

public static class IconThemeExtensions
{
	public static IObservable<IconTheme> ObserveChange(this IconTheme iconTheme) => iconTheme.Events().Changed.Select(_ => iconTheme);

	public static Pixbuf LoadIcon(this IconTheme iconTheme, string iconName, int size) => iconTheme.LoadIconByName(iconName, size);

	private static Pixbuf LoadIconByName(this IconTheme iconTheme, string iconName, int size)
	{
		Pixbuf imageBuffer = null;

		if (!string.IsNullOrEmpty(iconName))
		{
			if (iconName.StartsWith("/") && File.Exists(iconName))
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
}
