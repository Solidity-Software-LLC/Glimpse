using Gdk;

namespace Glimpse.Common.Images;

public class GtkGlimpseImage : IGlimpseImage
{
	public Pixbuf Pixbuf { get; set; }
	public int Width => Pixbuf.Width;
	public int Height => Pixbuf.Height;
	public double AspectRatio => (double)Pixbuf.Width / Pixbuf.Height;
	public void Dispose() => Pixbuf?.Dispose();

	public IGlimpseImage ScaleToFit(int maxHeight, int maxWidth)
	{
		var scaledWidth = maxHeight * AspectRatio;
		var scaledHeight = (double)maxHeight;

		if (scaledWidth > maxWidth)
		{
			scaledWidth = maxWidth;
			scaledHeight /= AspectRatio;
		}

		if (Width == (int)scaledWidth && Height == (int)scaledHeight)
		{
			return this;
		}

		return new GtkGlimpseImage { Pixbuf = Pixbuf.ScaleSimple((int)scaledWidth, (int)scaledHeight, InterpType.Bilinear) };
	}

	public IGlimpseImage Scale(int size) => new GtkGlimpseImage() { Pixbuf = Pixbuf.ScaleSimple(size, size, InterpType.Bilinear) };
}
