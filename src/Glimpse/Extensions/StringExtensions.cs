namespace Glimpse.Extensions;

public static class StringExtensions
{
	public static string RemoveObjectPath(this string s) => s.Split('/', StringSplitOptions.RemoveEmptyEntries).First();

	public static bool AllCharactersIn(this string source, string other)
	{
		var otherIndex = 0;

		if (source.Length == 0) return true;
		if (other.Length == 0) return false;

		foreach (var c in source)
		{
			if (otherIndex >= other.Length) return false;

			while (other[otherIndex++] != c)
			{
				if (otherIndex >= other.Length) return false;
			}
		}

		return true;
	}
}
