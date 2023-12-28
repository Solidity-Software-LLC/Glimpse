using Glimpse.Common.Images;

namespace Glimpse.UI.Components;

public class Assets
{
	public static readonly IGlimpseImage UnpinIcon;
	public static readonly IGlimpseImage PinIcon;
	public static readonly IGlimpseImage Close;
	public static readonly IGlimpseImage MenuIcon;
	public static readonly IGlimpseImage Power;
	public static readonly IGlimpseImage Settings;
	public static readonly IGlimpseImage Person;
	public static readonly IGlimpseImage Volume;
	public static readonly IGlimpseImage TaskManager;
	public static readonly IGlimpseImage MissingImage;
	public static readonly IGlimpseImage CaretUp;
	public static readonly IGlimpseImage CaretDown;

	static Assets()
	{
		UnpinIcon = LoadSvg("unpin.svg");
		PinIcon = LoadSvg("pin.svg");
		Close = LoadSvg("close.svg");
		MenuIcon = GlimpseImageFactory.FromResource("tux.png");
		Power = LoadSvg("power-outline.svg");
		Person = LoadSvg("person-circle-outline.svg");
		Settings = LoadSvg("settings-outline.svg");
		Volume = LoadSvg("volume-high.svg");
		TaskManager = LoadSvg("pulse-outline.svg");
		MissingImage = LoadSvg("missing-image.svg");
		CaretUp = LoadSvg("caret-up.svg");
		CaretDown = LoadSvg("caret-down.svg");
	}

	private static IGlimpseImage LoadSvg(string name)
	{
		return GlimpseImageFactory.FromResource(name);
	}
}
