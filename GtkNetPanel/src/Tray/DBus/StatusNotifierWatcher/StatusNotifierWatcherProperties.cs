using Tmds.DBus;

namespace GtkNetPanel.Tray;

[Dictionary]
internal class StatusNotifierWatcherProperties
{
	public string[] RegisteredStatusNotifierItems { get; set; } = default;
	public bool IsStatusNotifierHostRegistered { get; set; } = default;
	public int ProtocolVersion { get; set; } = default;
}
