using Gdk;
using Gtk;
using GtkNetPanel.Services.GtkSharp;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarBox : Box
{
	public ApplicationBarBox()
	{
		ShowAll();

		foreach (var window in Display.DefaultScreen.WindowStack.Where(w => w.TypeHint == WindowTypeHint.Normal))
		{
			// var state = window.GetAtomProperty(Atoms._NET_WM_STATE);
			// var iconName = window.GetStringProperty(Atoms._NET_WM_ICON_NAME);
			//var name = window.GetStringProperty(Atoms._NET_WM_NAME);
			var icons = window.GetIcons(Atoms._NET_WM_ICON);
			var biggestIcon = icons.MaxBy(i => i.Width);

			var colorCorrectedIconData = ConvertArgbToRgba(biggestIcon.Data, biggestIcon.Width, biggestIcon.Height);
			var imageBuffer = new Pixbuf(colorCorrectedIconData, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
			var image = new Image(imageBuffer.ScaleSimple(48, 48, InterpType.Bilinear));
			image.SetSizeRequest(52, 52);
			PackStart(image, false, false, 2);
		}
	}

	private static byte[] ConvertArgbToRgba(byte[] data, int width, int height)
	{
		var newArray = new byte[data.Length];
		Array.Copy(data, newArray, data.Length);

		for (var i = 0; i < sizeof(int) * width * height; i += 4)
		{
			var a = data[i];
			var r = data[i+1];
			var g = data[i+2];
			var b = data[i+3];

			newArray[i] = r;
			newArray[i + 1] = g;
			newArray[i + 2] = b;
			newArray[i + 3] = a;
		}

		return newArray;
	}
}
