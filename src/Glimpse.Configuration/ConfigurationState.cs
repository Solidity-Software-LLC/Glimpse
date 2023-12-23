using System.Collections.Immutable;
using Glimpse.Redux.Reducers;
using Glimpse.Redux.Selectors;
using static Glimpse.Redux.Selectors.SelectorFactory;

namespace Glimpse.Configuration;

internal class UpdateConfigurationAction
{
	public ConfigurationFile ConfigurationFile { get; set; }
}

public static class ConfigurationSelectors
{
	public static readonly ISelector<ConfigurationFile> Configuration = CreateFeatureSelector<ConfigurationFile>();
	public static readonly ISelector<string> VolumeCommand = CreateSelector(Configuration, s => s.VolumeCommand);
	public static readonly ISelector<string> TaskManagerCommand = CreateSelector(Configuration, s => s.TaskManagerCommand);
	public static readonly ISelector<ImmutableList<string>> TaskbarPinnedLaunchers = CreateSelector(Configuration, s => s.Taskbar.PinnedLaunchers);
	public static readonly ISelector<ImmutableList<StartMenuLaunchIconContextMenuItem>> StartMenuLaunchIconContextMenuItems = CreateSelector(Configuration, s => s.StartMenuLaunchIconContextMenu);
}

internal class AllReducers
{
	public static readonly FeatureReducerCollection Reducers = new()
	{
		FeatureReducer.Build(new ConfigurationFile())
			.On<UpdateConfigurationAction>((s, a) => a.ConfigurationFile)
	};
}
