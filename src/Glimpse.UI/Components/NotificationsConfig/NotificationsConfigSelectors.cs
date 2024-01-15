using System.Collections.Immutable;
using Glimpse.Configuration;
using Glimpse.Freedesktop;
using Glimpse.Freedesktop.Notifications;
using Glimpse.Redux.Selectors;
using Glimpse.UI.State;

namespace Glimpse.UI.Components.NotificationsConfig;

public record NotificationsConfigViewModel
{
	public ImmutableList<NotificationHistoryApplicationViewModel> KnownApplications { get; set; }

}

public record NotificationHistoryApplicationViewModel
{
	public string AppName { get; set; }
	public ImageViewModel AppIcon { get; set; }
}

public static class NotificationsConfigSelectors
{
	public static readonly ISelector<NotificationsConfigViewModel> ViewModel = SelectorFactory.CreateSelector(
		NotificationSelectors.KnownApplications,
		FreedesktopSelectors.AllDesktopFiles,
		ConfigurationSelectors.Configuration,
		(knownApplications, desktopFiles, config) =>
		{
			return new NotificationsConfigViewModel()
			{
				KnownApplications = knownApplications
					.Select(a =>
					{
						var desktopFile = desktopFiles.FirstOrDefault(d => d.FileName == a.DesktopEntry);
						var appIcon = desktopFile?.IconName ?? "";

						return new NotificationHistoryApplicationViewModel()
						{
							AppName = a.Name,
							AppIcon = new ImageViewModel() { IconNameOrPath = appIcon }
						};
					})
					.ToImmutableList()
			};

		});
}