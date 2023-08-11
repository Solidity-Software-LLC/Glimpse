using Gdk;

namespace GtkNetPanel.Services.GtkSharp;

public static class Atoms
{
	public static Atom WM_NAME = Atom.Intern("WM_NAME", true);

	public static Atom AnyPropertyType = new Atom(IntPtr.Zero);
	public static Atom STRING = Atom.Intern("STRING", true);
	public static Atom UTF8_STRING = Atom.Intern("UTF8_STRING", true);
	public static Atom COMPOUND_TEXT = Atom.Intern("COMPOUND_TEXT", true);

	public static Atom _NET_WM_STATE = Atom.Intern("_NET_WM_STATE", true);
	public static Atom _NET_WM_NAME = Atom.Intern("_NET_WM_NAME", true);
	public static Atom _NET_WM_ICON_NAME = Atom.Intern("_NET_WM_ICON_NAME", true);
	public static Atom _NET_WM_ICON = Atom.Intern("_NET_WM_ICON", true);
	public static Atom _NET_WM_PID = Atom.Intern("_NET_WM_PID", true);
}
