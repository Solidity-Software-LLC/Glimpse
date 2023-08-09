using Tmds.DBus;

namespace GtkNetPanel.DBus.Introspection;

[DBusInterface(DbusInterfaceName)]
public interface IIntrospectable : IDBusObject
{
	public const string DbusInterfaceName = "org.freedesktop.DBus.Introspectable";

	Task<string> IntrospectAsync();
}
