using GtkNetPanel.Services.DBus.Interfaces;

namespace GtkNetPanel.Services.DBus.StatusNotifierWatcher;

public record StatusNotifierItemProperties
{
	public string AttentionIconName;
	public StatusNotifierItemIconData[] AttentionIconPixmap;
	public string AttentionMovieName;
	public string Category;
	public string IconName;
	public StatusNotifierItemIconData[] IconPixmap;
	public string Id;
	public bool ItemIsMenu;
	public string OverlayIconName;
	public StatusNotifierItemIconData[] OverlayIconPixmap;
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
			IconPixmap = item.IconPixmap?.Select(i => new StatusNotifierItemIconData() { Width = i.Item1, Height = i.Item2, Data = i.Item3 }).ToArray(),
			OverlayIconName = item.OverlayIconName,
			OverlayIconPixmap = item.OverlayIconPixmap?.Select(i => new StatusNotifierItemIconData() { Width = i.Item1, Height = i.Item2, Data = i.Item3 }).ToArray(),
			AttentionIconName = item.AttentionIconName,
			AttentionIconPixmap = item.AttentionIconPixmap?.Select(i => new StatusNotifierItemIconData() { Width = i.Item1, Height = i.Item2, Data = i.Item3 }).ToArray(),
			AttentionMovieName = item.AttentionMovieName
		};
	}
}

public struct StatusNotifierItemIconData
{
	public int Height;
	public int Width;
	public byte[] Data;
}
