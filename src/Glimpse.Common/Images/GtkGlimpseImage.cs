using Gdk;

namespace Glimpse.Common.Images;

public class GtkGlimpseImage : IGlimpseImage
{
	public Pixbuf Image { get; init; }
	public int Width => Image.Width;
	public int Height => Image.Height;
	public void Dispose() => Image?.Dispose();

	public IGlimpseImage ScaleToFit(int maxWidth, int maxHeight)
	{
		return new GtkGlimpseImage { Image = Image.ScaleToFit(maxWidth, maxHeight) };
	}

	public IGlimpseImage Scale(int size) => new GtkGlimpseImage() { Image = Image.ScaleSimple(size, size, InterpType.Bilinear) };
}
