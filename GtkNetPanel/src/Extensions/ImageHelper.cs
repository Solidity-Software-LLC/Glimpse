namespace GtkNetPanel.Extensions;

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
}
