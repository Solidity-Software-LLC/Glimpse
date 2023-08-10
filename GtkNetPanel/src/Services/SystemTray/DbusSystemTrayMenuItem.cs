using System.Text;

namespace GtkNetPanel.Services.DBus.Menu;

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

	public static DbusSystemTrayMenuItem From((int, IDictionary<string, object>, object[]) root)
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
			Enabled = (bool?) enabled,
			Label = (string) label,
			Visible = (bool?) visible,
			IconName = (string) iconName,
			ToggleState = (int?) toggleState,
			ToggleType = (string) toggleType,
			Type = (string) type,
			IconData = (byte[]) iconData,
			Children = root.Item3.Cast<(int, IDictionary<string, object>, object[])>().Select(From).ToArray()
		};

		return item;
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
