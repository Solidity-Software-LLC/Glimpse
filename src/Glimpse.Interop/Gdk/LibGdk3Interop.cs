using System.Runtime.InteropServices;

namespace Glimpse.Interop.Gdk;

public class LibGdk3Interop
{
	private const string GdkNativeDll = "libgdk-3.so.0";
	private const string GioNativeDll = "libgio-2.0.so.0";

	[DllImport (GdkNativeDll, CallingConvention = CallingConvention.Cdecl)]
	public static extern ulong gdk_x11_window_get_xid (IntPtr windowHandle);

	[DllImport (GioNativeDll, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr g_desktop_app_info_new_from_filename(string fileName);

	[DllImport (GioNativeDll, CallingConvention = CallingConvention.Cdecl)]
	public static extern bool g_app_info_launch(IntPtr gDesktopFileInfo, IntPtr files, IntPtr context, IntPtr error);

	[DllImport (GioNativeDll, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_desktop_app_info_launch_action(IntPtr gDesktopFileInfo, string action, IntPtr context);
}
