using GtkNetPanel.DBus.Introspection;

namespace GtkNetPanel.DBus.StatusNotifierItem;

public record DbusStatusNotifierItem
{
	public StatusNotifierItemProperties Properties { get; set; }
	public DbusObject Object { get; set; }
	public DbusObject Menu { get; set; }
}
