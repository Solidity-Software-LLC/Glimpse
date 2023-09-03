namespace Glimpse.Extensions.IO;

public class IniSection
{
	public string Header { get; set; }
	public Dictionary<string, string> NameValuePairs { get; set; } = new();
}

public class IniFile
{
	public Dictionary<string, string> NameValuePairs { get; set; } = new();
	public LinkedList<IniSection> Sections { get; set; } = new();
	public string FilePath { get; set; }

	public static IniFile Read(Stream stream)
	{
		var config = new IniFile();
		IniSection currentSection = null;
		using var reader = new StreamReader(stream);


		while (reader.Peek() != -1)
		{
			var rawLine = reader.ReadLine()!; // Since Peak didn't return -1, stream hasn't ended.
			var line = rawLine.Trim();

			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			if (line[0] is ';' or '#' or '/')
			{
				continue;
			}

			if (line[0] == '[' && line[line.Length - 1] == ']')
			{
				currentSection = new IniSection() { Header = line.AsSpan(1, line.Length - 2).Trim().ToString() };
				config.Sections.AddLast(currentSection);
				continue;
			}

			var separator = line.IndexOf('=');
			if (separator < 0)
			{
				throw new FormatException("Invalid INI file");
			}

			var key = line.Substring(0, separator).Trim();
			var value = line.Substring(separator + 1).Trim();

			if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
			{
				value = value.Substring(1, value.Length - 2);
			}

			if (currentSection == null)
			{
				config.NameValuePairs[key] = value;
			}
			else
			{
				currentSection.NameValuePairs[key] = value;
			}
		}

		return config;
	}

}
