namespace GtkNetPanel.Extensions;

public static class StringExtensions
{
	public static string RemoveObjectPath(this string s) => s.Split('/', StringSplitOptions.RemoveEmptyEntries).First();
}
