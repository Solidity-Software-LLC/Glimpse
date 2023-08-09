using Tmds.DBus;

namespace GtkNetPanel.Tray;

[DBusInterface(DbusInterfaceName)]
public interface IIntrospectable : IDBusObject
{
	public const string DbusInterfaceName = "org.freedesktop.DBus.Introspectable";

	Task<string> IntrospectAsync();
}
