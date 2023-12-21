using Gdk;
using Glimpse.Extensions;

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
	public static readonly Pixbuf MissingImage;
	public static readonly Pixbuf CaretUp;
	public static readonly Pixbuf CaretDown;

	static Assets()
	{
		UnpinIcon = LoadSvg("unpin.svg");
		PinIcon = LoadSvg("pin.svg");
		Close = LoadSvg("close.svg");
		MenuIcon = Pixbuf.LoadFromResource("tux.png");
		Power = LoadSvg("power-outline.svg");
		Person = LoadSvg("person-circle-outline.svg");
		Settings = LoadSvg("settings-outline.svg");
		Volume = LoadSvg("volume-high.svg");
		TaskManager = LoadSvg("pulse-outline.svg");
		MissingImage = LoadSvg("missing-image.svg");
		CaretUp = LoadSvg("caret-up.svg");
		CaretDown = LoadSvg("caret-down.svg");
	}

	private static Pixbuf LoadSvg(string name)
	{
		var loader = PixbufLoader.LoadFromResource(name);
		var result = loader.Pixbuf;
		loader.Close();
		return result;
	}
}
