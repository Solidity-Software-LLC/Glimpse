using Glimpse.Common.Images;

namespace Glimpse.UI.Components;

public class Assets
{
	public static readonly IGlimpseImage MenuIcon;
	public static readonly IGlimpseImage Power;
	public static readonly IGlimpseImage Settings;
	public static readonly IGlimpseImage Person;

	static Assets()
	{
		MenuIcon = GlimpseImageFactory.FromResource("tux.png");
		Power = LoadSvg("power-outline.svg");
		Person = LoadSvg("person-circle-outline.svg");
		Settings = LoadSvg("settings-outline.svg");
	}

	private static IGlimpseImage LoadSvg(string name)
	{
		return GlimpseImageFactory.FromResource(name);
	}
}
