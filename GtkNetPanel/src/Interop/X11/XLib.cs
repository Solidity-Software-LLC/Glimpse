using System.Runtime.InteropServices;

namespace GtkNetPanel.Interop.X11;

public class XLib
{
	[DllImport("libX11")]
	public static extern ulong XOpenDisplay(ulong display);

	[DllImport("libX11")]
	public static extern int XCloseDisplay(ulong display);

	[DllImport("libX11")]
	public static extern int XSelectInput(ulong display, ulong window, EventMask event_mask);

	[DllImport("libX11")]
	public static extern int XNextEvent(ulong display, IntPtr e);

	[DllImport("libX11")]
	public static extern ulong XDefaultRootWindow(ulong display);

	[DllImport("libX11.so.6")]
	public static extern string XGetAtomName(ulong display, ulong atom);

	[DllImport("libX11.so.6")]
	public static extern void XFree(IntPtr data);

	[DllImport("libX11.so.6")]
	public static extern int XInitThreads();

	[DllImport("libX11.so.6", EntryPoint = "XQueryTree")]
	public static extern int XQueryTree(
		ulong display,    // Display* display
		ulong window,        // Window window
		out ulong root_return,
		out ulong parent_return,
		out IntPtr children_return, // Window* children_return
		out uint nchildren_return);

	[DllImport("libX11.so.6")]
	public static extern ulong XInternAtom(ulong display, string atom_name, bool only_if_exists);

	[DllImport("libX11.so.6")]
	public static extern int XGetWindowProperty(
		ulong display,
		ulong window,
		ulong property,
		long offset,
		long length,
		bool delete,
		ulong requestType,
		out ulong actualTypeReturn,
		out int actualFormatReturn,
		out ulong numItemsReturned,
		out ulong bytesLeft,
		out IntPtr data);

	[DllImport("libX11.so.6")]
	public static extern int XSetInputFocus(ulong display, ulong window, int revertTo, ulong time);

	[DllImport("libX11.so.6")]
	public static extern int XRaiseWindow(ulong display, ulong window);

	[DllImport("libX11.so.6")]
	public static extern int XLowerWindow(ulong display, ulong window);

	[DllImport("libX11.so.6")]
	public static extern int XGetInputFocus(ulong display, out ulong focus_return, out int revert_to_return);

	[DllImport("libX11.so.6")]
	public static extern int XMapWindow(ulong display, ulong window);

	[DllImport("libX11.so.6")]
	public static extern int XUnmapWindow(ulong display, ulong window);

	[DllImport("libX11.so.6")]
	public static extern int XFlush(ulong display);

	[DllImport("libX11.so.6")]
	public static extern int XIconifyWindow(ulong display, ulong window, int screen_number);

	[DllImport("libX11.so.6")]
	public static extern int XDefaultScreenOfDisplay(ulong display);

}

[Flags]
public enum EventMask : long
{
	NoEventMask = 0L,
	KeyPressMask = (1L << 0),
	KeyReleaseMask = (1L << 1),
	ButtonPressMask = (1L << 2),
	ButtonReleaseMask = (1L << 3),
	EnterWindowMask = (1L << 4),
	LeaveWindowMask = (1L << 5),
	PointerMotionMask = (1L << 6),
	PointerMotionHintMask = (1L << 7),
	Button1MotionMask = (1L << 8),
	Button2MotionMask = (1L << 9),
	Button3MotionMask = (1L << 10),
	Button4MotionMask = (1L << 11),
	Button5MotionMask = (1L << 12),
	ButtonMotionMask = (1L << 13),
	KeymapStateMask = (1L << 14),
	ExposureMask = (1L << 15),
	VisibilityChangeMask = (1L << 16),
	StructureNotifyMask = (1L << 17),
	ResizeRedirectMask = (1L << 18),
	SubstructureNotifyMask = (1L << 19),
	SubstructureRedirectMask = (1L << 20),
	FocusChangeMask = (1L << 21),
	PropertyChangeMask = (1L << 22),
	ColormapChangeMask = (1L << 23),
	OwnerGrabButtonMask = (1L << 24),
}

public enum Event : int
{
	KeyPress = 2,
	KeyRelease = 3,
	ButtonPress = 4,
	ButtonRelease = 5,
	MotionNotify = 6,
	EnterNotify = 7,
	LeaveNotify = 8,
	FocusIn = 9,
	FocusOut = 10,
	KeymapNotify = 11,
	Expose = 12,
	GraphicsExpose = 13,
	NoExpose = 14,
	VisibilityNotify = 15,
	CreateNotify = 16,
	DestroyNotify = 17,
	UnmapNotify = 18,
	MapNotify = 19,
	MapRequest = 20,
	ReparentNotify = 21,
	ConfigureNotify = 22,
	ConfigureRequest = 23,
	GravityNotify = 24,
	ResizeRequest = 25,
	CirculateNotify = 26,
	CirculateRequest = 27,
	PropertyNotify = 28,
	SelectionClear = 29,
	SelectionRequest = 30,
	SelectionNotify = 31,
	ColormapNotify = 32,
	ClientMessage = 33,
	MappingNotify = 34,
	GenericEvent = 35,
	LASTEvent = 36
}

[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XAnyEvent
{
	public int type;
	public ulong serial;
	public bool send_event;
	public ulong display;
	public ulong window;
}

[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XKeyEvent
{
	public int type; /* of event */
	public ulong serial; /* # of last request processed by server */
	public bool send_event; /* true if this came from a SendEvent request */
	public ulong display; /* Display the event was read from */
	public ulong window; /* "event" window it is reported relative to */
	public ulong root; /* root window that the event occurred on */
	public ulong subwindow; /* child window */
	public ulong time; /* milliseconds */
	public int x, y; /* pointer x, y coordinates in event window */
	public int x_root, y_root; /* coordinates relative to root */
	public uint state; /* key or button mask */
	public uint keycode; /* detail */
	public bool same_screen;
}

[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XFocusChangeEvent
{
	public int type; /* FocusIn or FocusOut */
	public ulong serial; /* # of last request processed by server */
	public bool send_event; /* true if this came from a SendEvent request */
	public ulong display; /* Display the event was read from */
	public ulong window; /* window of event */
	NotifyMode mode;
	NotifyDetail detail;
}

[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XExposeEvent
{
	public int type;
	public ulong serial; /* # of last request processed by server */
	public bool send_event; /* true if this came from a SendEvent request */
	public ulong display; /* Display the event was read from */
	public ulong window;
	public int x, y;
	public int width, height;
	public int count;
}


[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XVisibilityEvent
{
	public int type;
	public ulong serial; /* # of last request processed by server */
	public bool send_event; /* true if this came from a SendEvent request */
	public ulong display; /* Display the event was read from */
	public ulong window;
	public int state;
}

[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XCreateWindowEvent
{
	public int type;
	public ulong serial; /* # of last request processed by server */
	public bool send_event; /* true if this came from a SendEvent request */
	public ulong display; /* Display the event was read from */
	public ulong parent; /* parent of the window */
	public ulong window; /* window id of window created */
	public int x, y; /* window location */
	public int width, height; /* size of window */
	public int border_width; /* border width */
	public bool override_redirect;
}


[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XPropertyEvent
{
	public int type;
	public ulong serial;   /* # of last request processed by server */
	public bool send_event;        /* true if this came from a SendEvent request */
	public ulong display;       /* Display the event was read from */
	public ulong window;
	public ulong atom;
	public long time;
	public int state;
}

[StructLayout(LayoutKind.Sequential, Size = (24 * sizeof(long)))]
public struct XDestroyWindowEvent
{
	public int type;
	public ulong serial; /* # of last request processed by server */
	public bool send_event; /* true if this came from a SendEvent request */
	public ulong display; /* Display the event was read from */
	public IntPtr @event;
	public ulong window;
}

public enum NotifyMode : int
{
	NotifyNormal = 0,
	NotifyGrab = 1,
	NotifyUngrab = 2,
	NotifyWhileGrabbed = 3,
}

public enum NotifyDetail : int
{
	NotifyAncestor = 0,
	NotifyVirtual = 1,
	NotifyInferior = 2,
	NotifyNonlinear = 3,
	NotifyNonlinearVirtual = 4,
	NotifyPointer = 5,
	NotifyPointerRoot = 6,
	NotifyDetailNone = 7,
}
