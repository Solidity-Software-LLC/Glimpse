using Gdk;

namespace GtkNetPanel.Components;

public class Assets
{
	public static readonly Pixbuf UnpinIcon;
	public static readonly Pixbuf PinIcon;
	public static readonly Pixbuf Close;
	public static readonly Pixbuf MenuIcon;

	static Assets()
	{
		UnpinIcon = Pixbuf.LoadFromResource("unpin.png");
		PinIcon = Pixbuf.LoadFromResource("pin.png");
		Close = Pixbuf.LoadFromResource("close.png");
		MenuIcon = Pixbuf.LoadFromResource("tux.png");
	}
}
