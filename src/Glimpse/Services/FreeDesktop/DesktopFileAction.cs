using Glimpse.Extensions.IO;

namespace Glimpse.Services.FreeDesktop;

public class DesktopFileAction
{
	public string Id { get; set; }
	public string ActionName { get; set; }
	public string IconName { get; set; }
	public string Exec { get; set; }
	public string DesktopFilePath { get; set; }

	public static DesktopFileAction Parse(IniSection section, string desktopFilePath)
	{
		section.NameValuePairs.TryGetValue("Name", out var actionName);
		section.NameValuePairs.TryGetValue("Exec", out var exec);
		section.NameValuePairs.TryGetValue("Icon", out var iconName);

		return new DesktopFileAction()
		{
			Id = section.Header.Split(" ").Last(),
			ActionName = actionName,
			IconName = iconName,
			Exec = exec,
			DesktopFilePath = desktopFilePath
		};
	}
}
