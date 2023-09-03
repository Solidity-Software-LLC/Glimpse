namespace Glimpse.Services.DBus.Core;

public class DBusDoubleItem : DBusBasicTypeItem
{
	public DBusDoubleItem(double value) => Value = value;

	public double Value { get; }
}
