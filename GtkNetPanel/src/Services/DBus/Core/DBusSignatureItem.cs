using Tmds.DBus.Protocol;

namespace GtkNetPanel.Services.DBus.Core;

public class DBusSignatureItem : DBusBasicTypeItem
{
	public DBusSignatureItem(Signature value) => Value = value;

	public Signature Value { get; }
}
