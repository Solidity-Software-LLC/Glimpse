using System.Collections.Immutable;
using Glimpse.Configuration;
using Glimpse.Lib.System.Collections.Immutable;
using Glimpse.Redux.Effects;
using static Glimpse.Redux.Effects.EffectsFactory;

namespace Glimpse.UI.State;

public class UIEffects(ConfigurationService configurationService) : IEffectsFactory
{
	public IEnumerable<Effect> Create() => new[]
	{
		CreateEffect<ToggleTaskbarPinningAction, ConfigurationFile>(
			ConfigurationSelectors.Configuration,
			(a, s) =>
			{
				configurationService.UpdateConfiguration(s with { Taskbar = s.Taskbar with { PinnedLaunchers = s.Taskbar.PinnedLaunchers.Toggle(a.DesktopFileId) } });
			}),
		CreateEffect<ToggleStartMenuPinningAction, ConfigurationFile>(
			ConfigurationSelectors.Configuration,
			(a, s) =>
			{
				configurationService.UpdateConfiguration(s with { StartMenu = s.StartMenu with { PinnedLaunchers = s.StartMenu.PinnedLaunchers.Toggle(a.DesktopFileId) } });
			}),
		CreateEffect<UpdateStartMenuPinnedAppOrderingAction, ConfigurationFile>(
			ConfigurationSelectors.Configuration,
			(a, s) =>
			{
				if (s.StartMenu.PinnedLaunchers.SequenceEqual(a.DesktopFileKeys)) return;
				configurationService.UpdateConfiguration(s with { StartMenu = s.StartMenu with { PinnedLaunchers = a.DesktopFileKeys } });
			}),
		CreateEffect<UpdateTaskbarSlotOrderingBulkAction, ConfigurationFile>(
			ConfigurationSelectors.Configuration,
			(a, s) =>
			{
				var pinnedSlots = a.Slots.Select(r => r.PinnedDesktopFileId).Where(slot => !string.IsNullOrEmpty(slot)).ToImmutableList();
				if (pinnedSlots.SequenceEqual(s.Taskbar.PinnedLaunchers)) return;
				configurationService.UpdateConfiguration(s with { Taskbar = s.Taskbar with { PinnedLaunchers = pinnedSlots } });
			})
	};
}
