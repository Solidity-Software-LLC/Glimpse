using Gdk;

namespace Glimpse.Components;

public class Assets
{
	public static readonly Pixbuf UnpinIcon;
	public static readonly Pixbuf PinIcon;
	public static readonly Pixbuf Close;
	public static readonly Pixbuf MenuIcon;
	public static readonly Pixbuf Power;
	public static readonly Pixbuf Settings;
	public static readonly Pixbuf Person;
	public static readonly Pixbuf Volume;
	public static readonly Pixbuf TaskManager;

	static Assets()
	{
		UnpinIcon = Pixbuf.LoadFromResource("unpin.png");
		PinIcon = Pixbuf.LoadFromResource("pin.png");
		Close = Pixbuf.LoadFromResource("close.png");
		MenuIcon = Pixbuf.LoadFromResource("tux.png");
		Power = LoadSvg("power-outline.svg");
		Person = LoadSvg("person-circle-outline.svg");
		Settings = LoadSvg("settings-outline.svg");
		Volume = LoadSvg("volume-high.svg");
		TaskManager = LoadSvg("pulse-outline.svg");
	}

	private static Pixbuf LoadSvg(string name)
	{
		var loader = PixbufLoader.LoadFromResource(name);
		var result = loader.Pixbuf;
		loader.Close();
		return result;
	}
}
