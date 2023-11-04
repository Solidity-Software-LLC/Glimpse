namespace Glimpse.State;

public class BitmapImage
{
	public int Width { get; init; }
	public int Height { get; init; }
	public byte[] Data { get; init; }
	public int Depth { get; init; }

	public static BitmapImage Empty = new() { Data = Array.Empty<byte>(), Depth = 32, Height = 1, Width = 1 };
}
