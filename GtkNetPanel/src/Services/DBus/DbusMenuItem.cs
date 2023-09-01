using GtkNetPanel.Services.DBus.Core;

namespace GtkNetPanel.Services.DBus;

public class DbusMenuItem
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

	public DbusMenuItem[] Children { get; set; }

	public static DbusMenuItem From((int, Dictionary<string, DBusVariantItem>, DBusVariantItem[]) root)
	{
		root.Item2.TryGetValue("enabled", out var enabled);
		root.Item2.TryGetValue("label", out var label);
		root.Item2.TryGetValue("visible", out var visible);
		root.Item2.TryGetValue("icon-name", out var iconName);
		root.Item2.TryGetValue("toggle-state", out var toggleState);
		root.Item2.TryGetValue("toggle-type", out var toggleType);
		root.Item2.TryGetValue("icon-data", out var iconData);
		root.Item2.TryGetValue("type", out var type);


		var item = new DbusMenuItem()
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

	private static DbusMenuItem[] ProcessChildren(DBusVariantItem[] children)
	{
		if (!children.Any()) return Array.Empty<DbusMenuItem>();

		var processedChildren = new LinkedList<DbusMenuItem>();

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
}
