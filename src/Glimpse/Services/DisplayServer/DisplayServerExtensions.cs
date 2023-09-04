using Glimpse.Services.X11;

namespace Glimpse.Services.DisplayServer;

public static class DisplayServerExtensions
{
	public static GenericWindowRef ToGenericReference(this XWindowRef windowRef)
	{
		return new GenericWindowRef() { Id = $"{windowRef.Display:X}_{windowRef.Window:X}", InternalRef = windowRef };
	}
}
