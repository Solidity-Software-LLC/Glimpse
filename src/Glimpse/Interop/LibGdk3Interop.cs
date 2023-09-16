using System.Runtime.InteropServices;

namespace Glimpse.Interop;

public class LibGdk3Interop
{
	private const string GdkNativeDll = "libgdk-3";

	[DllImport (GdkNativeDll, CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong gdk_x11_window_get_xid (IntPtr windowHandle);
}
