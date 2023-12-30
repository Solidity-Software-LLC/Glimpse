using Glimpse.Common.Images;
using Glimpse.Freedesktop.DBus.Interfaces;

namespace Glimpse.Freedesktop.DBus;

public record StatusNotifierItemProperties
{
	public string AttentionIconName;
	public IGlimpseImage[] AttentionIconPixmap;
	public string AttentionMovieName;
	public string Category;
	public string IconName;
	public IGlimpseImage[] IconPixmap;
	public string Id;
	public bool ItemIsMenu;
	public string OverlayIconName;
	public IGlimpseImage[] OverlayIconPixmap;
	public string Status;
	public string Title;
	public string IconThemePath;
	public string MenuPath;

	internal static StatusNotifierItemProperties From(OrgKdeStatusNotifierItem.Properties item)
	{
		return new StatusNotifierItemProperties()
		{
			Category = item.Category,
			Id = item.Id,
			Title = item.Title,
			Status = item.Status,
			IconThemePath = item.IconThemePath,
			ItemIsMenu = item.ItemIsMenu,
			IconName = item.IconName,
			MenuPath = item.Menu.ToString(),
			IconPixmap = item.IconPixmap?.Select(i => GlimpseImageFactory.From(ImageHelper.ConvertArgbToRgba(i.Item3, i.Item1, i.Item2), 32, i.Item1, i.Item2)).ToArray(),
			OverlayIconName = item.OverlayIconName,
			OverlayIconPixmap = item.OverlayIconPixmap?.Select(i => GlimpseImageFactory.From(ImageHelper.ConvertArgbToRgba(i.Item3, i.Item1, i.Item2), 32, i.Item1, i.Item2)).ToArray(),
			AttentionIconName = item.AttentionIconName,
			AttentionIconPixmap = item.AttentionIconPixmap?.Select(i => GlimpseImageFactory.From(ImageHelper.ConvertArgbToRgba(i.Item3, i.Item1, i.Item2), 32, i.Item1, i.Item2)).ToArray(),
			AttentionMovieName = item.AttentionMovieName
		};
	}
}
