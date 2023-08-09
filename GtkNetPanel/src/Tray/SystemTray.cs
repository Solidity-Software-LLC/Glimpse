using Gdk;
using Gtk;
using GtkNetPanel.Tray;

namespace GtkNetPanel;

public class SystemTray : HBox
{
	private List<StatusNotifierItem> _statusNotifierItems;

	protected override void OnShown()
	{
		Task.Run(async () =>
		{
			try
			{
				_statusNotifierItems = await DBus.GetTrayItems();

				foreach (var item in _statusNotifierItems)
				{
					var image = LoadImage(item.Properties);
					if (image == null) continue;
					var systemTrayIcon = new SystemTrayIcon(item, image);
					PackStart(systemTrayIcon, false, false, 3);
				}

				ShowAll();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		});

		base.OnShown();
	}

	private byte[] ConvertArgbToRgba(byte[] data, int width, int height)
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

	// Move this into the icon class
	private Image LoadImage(StatusNotifierItemProperties item)
	{
		if (!string.IsNullOrEmpty(item.IconThemePath))
		{
			var imageData = File.ReadAllBytes(System.IO.Path.Join(item.IconThemePath, item.IconName) +  ".png");
			var loader = PixbufLoader.NewWithType("png");
			loader.Write(imageData);

			return new Image(loader.Pixbuf.ScaleSimple(24, 24, InterpType.Bilinear));
		}

		if (!string.IsNullOrEmpty(item.IconName))
		{
			var iconTheme = IconTheme.GetForScreen(Screen);
			var pixbuf = iconTheme.LoadIcon(item.IconName, 24, IconLookupFlags.DirLtr);
			pixbuf = pixbuf.ScaleSimple(24, 24, InterpType.Bilinear);

			return new Image(pixbuf);
		}

		if (item.IconPixmap != null)
		{
			var biggestIcon = item.IconPixmap.MaxBy(i => i.Width * i.Height);
			var colorCorrectedIconData = ConvertArgbToRgba(biggestIcon.Data, biggestIcon.Width, biggestIcon.Height);
			var pixBuffer = new Pixbuf(colorCorrectedIconData, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, 4 * biggestIcon.Width);
			return new Image(pixBuffer.ScaleSimple(24, 24, InterpType.Bilinear));
		}

		Console.WriteLine("System Tray - Failed to find icon for: " + item.Title);
		return null;
	}
}
