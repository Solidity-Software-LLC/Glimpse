using Glimpse.Services.DisplayServer;

namespace Glimpse.Services.X11;

public struct XWindowRef : IWindowRef, IEquatable<XWindowRef>
{
	public ulong Id => Window;
	public ulong Window { get; init; }
	public ulong Display { get; init; }

	public bool Equals(XWindowRef other)
	{
		return Id == other.Id;
	}
}
