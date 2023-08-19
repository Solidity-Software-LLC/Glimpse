namespace GtkNetPanel.Interop.X11;

public class XAtom
{
	public XAtom(ulong atomId, string name)
	{

	}
}

public static class XAtoms
{
	public static readonly ulong String;
	public static readonly ulong Utf8String;
	public static readonly ulong CompoundText;
	public static readonly ulong WmName;
	public static readonly ulong NetWmState;
	public static readonly ulong NetWmName;
	public static readonly ulong NetWmIconName;
	public static readonly ulong NetWmIcon;
	public static readonly ulong NetWmPid;
	public static readonly ulong NetWmWindowType;
	public static readonly ulong NetClientList;
	public static readonly ulong WinClientList;
	public static readonly ulong NetWmWindowTypeNormal;
	public static readonly ulong WmChangeState;
	public static readonly ulong NetWmAllowedActions;
	public static readonly ulong NetCloseWindow;

	static XAtoms()
	{
		var display = XLib.XOpenDisplay(0);

		String = XLib.XInternAtom(display, "STRING", true);
		Utf8String = XLib.XInternAtom(display, "UTF8_STRING", true);
		CompoundText = XLib.XInternAtom(display, "COMPOUND_TEXT", true);
		WmName = XLib.XInternAtom(display, "WM_NAME", true);
		NetWmState = XLib.XInternAtom(display, "_NET_WM_STATE", true);
		NetWmName = XLib.XInternAtom(display, "_NET_WM_NAME", true);
		NetWmIconName = XLib.XInternAtom(display, "_NET_WM_ICON_NAME", true);
		NetWmIcon = XLib.XInternAtom(display, "_NET_WM_ICON", true);
		NetWmPid = XLib.XInternAtom(display, "_NET_WM_PID", true);
		NetWmWindowType = XLib.XInternAtom(display, "_NET_WM_WINDOW_TYPE", true);
		NetClientList = XLib.XInternAtom(display, "_NET_CLIENT_LIST", true);
		WinClientList = XLib.XInternAtom(display, "_WIN_CLIENT_LIST", true);
		NetWmWindowTypeNormal = XLib.XInternAtom(display, "_NET_WM_WINDOW_TYPE_NORMAL", true);
		WmChangeState = XLib.XInternAtom(display, "WM_CHANGE_STATE", true);
		NetWmAllowedActions = XLib.XInternAtom(display, "_NET_WM_ALLOWED_ACTIONS", true);
		NetCloseWindow = XLib.XInternAtom(display, "_NET_CLOSE_WINDOW", true);

		XLib.XCloseDisplay(display);
	}
}
