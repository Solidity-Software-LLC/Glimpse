using Tmds.DBus.Protocol;

namespace Glimpse.Services.DBus.Core;

public class DBusSignatureItem : DBusBasicTypeItem
{
	public DBusSignatureItem(Signature value) => Value = value;

	public Signature Value { get; }
}
