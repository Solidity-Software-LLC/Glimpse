using Glimpse.Extensions.IO;

namespace Glimpse.Services.FreeDesktop;

public class DesktopFileAction
{
	public string ActionName { get; set; }
	public string IconName { get; set; }
	public string Exec { get; set; }

	public static DesktopFileAction Parse(IniSection section)
	{
		section.NameValuePairs.TryGetValue("Name", out var actionName);
		section.NameValuePairs.TryGetValue("Exec", out var exec);
		section.NameValuePairs.TryGetValue("Icon", out var iconName);

		return new DesktopFileAction()
		{
			ActionName = actionName,
			IconName = iconName,
			Exec = exec
		};
	}
}
