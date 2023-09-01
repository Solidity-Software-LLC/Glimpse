namespace GtkNetPanel.Services.DBus.Core;

public class DBusUInt64Item : DBusBasicTypeItem
{
	public DBusUInt64Item(ulong value) => Value = value;

	public ulong Value { get; }
}
