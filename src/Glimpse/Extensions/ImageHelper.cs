using Cairo;
using Gdk;

namespace Glimpse.Extensions;

public static class ImageHelper
{
	public static byte[] ConvertArgbToRgba(byte[] data, int width, int height)
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

	public static Pixbuf CreatePixbuf(byte[] data, int depth, int width, int height)
	{
		var surface = new ImageSurface(data, depth == 24 ? Format.RGB24 : Format.Argb32, width, height, 4 * width);
		var buffer = new Pixbuf(surface, 0, 0, width, height);
		surface.Dispose();
		return buffer;
	}

	public static double AspectRatio(this Pixbuf image)
	{
		return (double)image.Width / image.Height;
	}

	public static Pixbuf ScaleToFit(this Pixbuf imageBuffer, int maxHeight, int maxWidth)
	{
		var scaledWidth = maxHeight * imageBuffer.AspectRatio();
		var scaledHeight = (double) maxHeight;

		if (scaledWidth > maxWidth)
		{
			scaledWidth = maxWidth;
			scaledHeight /= imageBuffer.AspectRatio();
		}

		if (imageBuffer.Width == (int) scaledWidth && imageBuffer.Height == (int) scaledHeight)
		{
			return imageBuffer;
		}

		return imageBuffer.ScaleSimple((int) scaledWidth, (int) scaledHeight, InterpType.Bilinear);
	}

	public static Pixbuf Scale(this Pixbuf image, int size)
	{
		return image.ScaleSimple(size, size, InterpType.Bilinear);
	}
}
