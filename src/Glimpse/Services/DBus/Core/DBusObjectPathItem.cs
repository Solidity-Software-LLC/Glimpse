using Tmds.DBus.Protocol;

namespace Glimpse.Services.DBus.Core;

public class DBusObjectPathItem : DBusBasicTypeItem
{
	public DBusObjectPathItem(ObjectPath value) => Value = value;

	public ObjectPath Value { get; }
}
