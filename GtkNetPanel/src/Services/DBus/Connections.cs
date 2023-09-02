using Tmds.DBus.Protocol;

namespace GtkNetPanel.Services.DBus;

public class Connections
{
	public Connection Session { get; set; }
	public Connection System { get; set; }
}
