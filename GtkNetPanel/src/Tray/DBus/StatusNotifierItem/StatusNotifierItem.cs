namespace GtkNetPanel.Tray;

public record StatusNotifierItem
{
	public StatusNotifierItemProperties Properties { get; set; }
	public DbusObject Object { get; set; }
	public DbusObject Menu { get; set; }
}
