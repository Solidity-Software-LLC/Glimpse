namespace GtkNetPanel.Services.DBus.Core;

public class DBusUInt16Item : DBusBasicTypeItem
{
	public DBusUInt16Item(ushort value) => Value = value;

	public ushort Value { get; }
}
