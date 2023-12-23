namespace Glimpse.Images;

public interface IGlimpseImage : IDisposable
{
	int Width { get; }
	int Height { get; }
	double AspectRatio { get; }
	IGlimpseImage ScaleToFit(int maxHeight, int maxWidth);
	IGlimpseImage Scale(int size);
}
