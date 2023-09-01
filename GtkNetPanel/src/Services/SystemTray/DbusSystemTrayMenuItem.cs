using System.Text;
using GtkNetPanel.Services.DBus.Core;

namespace GtkNetPanel.Services.SystemTray;

public class DbusSystemTrayMenuItem
{
	public int Id { get; set; }
	public bool? Enabled { get; set; }
	public string Label { get; set; }
	public bool? Visible { get; set; }
	public string IconName { get; set; }
	public int? ToggleState { get; set; }
	public string ToggleType { get; set; }
	public string Type { get; set; }
	public byte[] IconData { get; set; }

	public DbusSystemTrayMenuItem[] Children { get; set; }

	public static DbusSystemTrayMenuItem From((int, Dictionary<string, DBusVariantItem>, DBusVariantItem[]) root)
	{
		root.Item2.TryGetValue("enabled", out var enabled);
		root.Item2.TryGetValue("label", out var label);
		root.Item2.TryGetValue("visible", out var visible);
		root.Item2.TryGetValue("icon-name", out var iconName);
		root.Item2.TryGetValue("toggle-state", out var toggleState);
		root.Item2.TryGetValue("toggle-type", out var toggleType);
		root.Item2.TryGetValue("icon-data", out var iconData);
		root.Item2.TryGetValue("type", out var type);


		var item = new DbusSystemTrayMenuItem()
		{
			Id = root.Item1,
			Enabled = ((DBusBoolItem) enabled?.Value)?.Value,
			Label = ((DBusStringItem) label?.Value)?.Value,
			Visible = ((DBusBoolItem) visible?.Value)?.Value,
			IconName = ((DBusStringItem) iconName?.Value)?.Value,
			ToggleState = ((DBusInt32Item) toggleState?.Value)?.Value,
			ToggleType = ((DBusStringItem) toggleType?.Value)?.Value,
			Type = ((DBusStringItem) type?.Value)?.Value,
			Children = ProcessChildren(root.Item3)
		};

		if (iconData?.Value is DBusArrayItem iconArray)
		{
			item.IconData = iconArray.Select(i => i as DBusByteItem).Select(i => i.Value).ToArray();
		}
		else
		{
			item.IconData = ((DBusByteArrayItem) iconData?.Value)?.ToArray();
		}

		return item;
	}

	private static DbusSystemTrayMenuItem[] ProcessChildren(DBusVariantItem[] children)
	{
		if (!children.Any()) return Array.Empty<DbusSystemTrayMenuItem>();

		var processedChildren = new LinkedList<DbusSystemTrayMenuItem>();

		foreach (var child in children.Select(c => c.Value as DBusStructItem))
		{
			var id = ((DBusInt32Item) child.First()).Value;
			var properties = ((DBusArrayItem)child.ElementAt(1))
				.ToArray()
				.Cast<DBusDictEntryItem>()
				.ToDictionary(i => i.Key.ToString(), i => (DBusVariantItem) i.Value);
			var childrenOfChild = ((DBusArrayItem)child.ElementAt(2)).Cast<DBusVariantItem>().ToArray();
			var tuple = (id, properties, subChildren: childrenOfChild);

			processedChildren.AddLast(From(tuple));

		}

		return processedChildren.ToArray();
	}

	public string Print(int depth)
	{
		var output = new StringBuilder();

		for (var i = 0; i < depth; i++) output.Append("\t");
		output.AppendLine($"Id: {Id}");
		for (var i = 0; i < depth; i++) output.Append("\t");
		output.AppendLine($"Enabled: {Enabled}");
		for (var i = 0; i < depth; i++) output.Append("\t");
		output.AppendLine($"Label: {Label}");
		for (var i = 0; i < depth; i++) output.Append("\t");
		output.AppendLine($"Visible: {Visible}");
		for (var i = 0; i < depth; i++) output.Append("\t");
		output.AppendLine($"IconName: {IconName}");
		for (var i = 0; i < depth; i++) output.Append("\t");
		output.AppendLine($"ToggleState: {ToggleState}");
		for (var i = 0; i < depth; i++) output.Append("\t");
		output.AppendLine($"ToggleType: {ToggleType}");

		foreach (var c in Children)
		{
			output.AppendLine(c.Print(depth + 1));
		}

		return output.ToString();
	}
}
