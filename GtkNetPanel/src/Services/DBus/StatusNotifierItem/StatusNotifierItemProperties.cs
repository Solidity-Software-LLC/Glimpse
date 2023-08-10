using Tmds.DBus;

namespace GtkNetPanel.Services.DBus.StatusNotifierItem;

[Dictionary]
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
}

public struct StatusNotifierItemIconData
{
	public int Height;
	public int Width;
	public byte[] Data;
}
