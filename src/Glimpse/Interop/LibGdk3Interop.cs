using System.Runtime.InteropServices;

namespace Glimpse.Interop;

public class LibGdk3Interop
{
	private const string GdkNativeDll = "libgdk-3";
	private const string GioNativeDll = "libgio-2.0";

	[DllImport (GdkNativeDll, CallingConvention = CallingConvention.Cdecl)]
	internal static extern ulong gdk_x11_window_get_xid (IntPtr windowHandle);

	[DllImport (GioNativeDll, CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr g_desktop_app_info_new_from_filename(string fileName);

	[DllImport (GioNativeDll, CallingConvention = CallingConvention.Cdecl)]
	internal static extern bool g_app_info_launch(IntPtr gDesktopFileInfo, IntPtr files, IntPtr context, IntPtr error);

	[DllImport (GioNativeDll, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void g_desktop_app_info_launch_action(IntPtr gDesktopFileInfo, string action, IntPtr context);
}
