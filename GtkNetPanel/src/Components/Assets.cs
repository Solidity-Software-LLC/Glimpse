using Gdk;

namespace GtkNetPanel.Components;

public class Assets
{
	public static readonly Pixbuf UnpinIcon;
	public static readonly Pixbuf PinIcon;
	public static readonly Pixbuf Close;

	static Assets()
	{
		UnpinIcon = Pixbuf.LoadFromResource("GtkNetPanel.unpin.png");
		PinIcon = Pixbuf.LoadFromResource("GtkNetPanel.pin.png");
		Close = Pixbuf.LoadFromResource("GtkNetPanel.close.png");
	}
}
