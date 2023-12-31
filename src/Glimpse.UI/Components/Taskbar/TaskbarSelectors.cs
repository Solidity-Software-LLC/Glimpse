using System.Collections.Immutable;
using Glimpse.Common.Images;
using Glimpse.Common.System.Collections;
using Glimpse.Freedesktop;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Redux.Selectors;
using Glimpse.UI.State;
using Glimpse.Xorg;
using Glimpse.Xorg.State;
using static Glimpse.Redux.Selectors.SelectorFactory;

namespace Glimpse.UI.Components.Taskbar;

public static class TaskbarSelectors
{
	private static readonly ISelector<ImmutableList<WindowProperties>> s_windowPropertiesList = CreateSelector(
		XorgSelectors.Windows,
		windows => windows.ById.Values.ToImmutableList());

	public static readonly ISelector<SlotReferences> Slots = CreateSelector(
		FreedesktopSelectors.DesktopFiles,
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef.Id == y.WindowRef.Id && x.ClassHintName == y.ClassHintName),
		UISelectors.UserSortedSlots,
		(desktopFiles, windows, userSortedSlotCollection) =>
		{
			var result = ImmutableList<SlotRef>.Empty.AddRange(userSortedSlotCollection.Refs);

			var previouslyFoundDesktopFiles = userSortedSlotCollection.Refs
				.Select(l => desktopFiles.ContainsKey(l.PinnedDesktopFileId) ? desktopFiles.ById[l.PinnedDesktopFileId] : null)
				.Where(f => f != null)
				.ToList();

			foreach (var window in windows.OrderBy(w => w.CreationDate))
			{
				if (result.Any(s => s.ClassHintName == window.ClassHintName)) continue;
				var desktopFile = FindAppDesktopFileByName(previouslyFoundDesktopFiles, window.ClassHintName) ?? FindAppDesktopFileByName(desktopFiles.ById.Values, window.ClassHintName);
				var matchingSlot = result.FirstOrDefault(s => s.PinnedDesktopFileId == desktopFile?.Id) ?? result.FirstOrDefault(s => s.DiscoveredDesktopFileId == desktopFile?.Id);

				if (matchingSlot == null)
				{
					result = result.Add(new SlotRef() { ClassHintName = window.ClassHintName, DiscoveredDesktopFileId = desktopFile?.Id ?? "" });
				}
				else if (string.IsNullOrEmpty(matchingSlot.ClassHintName))
				{
					result = result.Replace(matchingSlot, matchingSlot with { ClassHintName = window.ClassHintName });
				}
			}

			return new SlotReferences { Refs = result };
		});

	private static readonly ISelector<ImmutableList<(SlotRef Slot, DesktopFile DesktopFile)>> s_slotToDesktopFile = CreateSelector(
		Slots,
		FreedesktopSelectors.DesktopFiles,
		(slots, desktopFiles) =>
		{
			return slots.Refs.Select(slot => (
					Slot: slot,
					DesktopFile: desktopFiles.ContainsKey(slot.PinnedDesktopFileId) ? desktopFiles.ById[slot.PinnedDesktopFileId]
					: desktopFiles.ContainsKey(slot.DiscoveredDesktopFileId) ? desktopFiles.ById[slot.DiscoveredDesktopFileId]
					: new DesktopFile() { Id = "" }))
				.ToImmutableList();
		},
		(x, y) => CollectionComparer.Sequence(x, y, (i, j) => i.Slot == j.Slot && i.DesktopFile.Id == j.DesktopFile.Id));

	private static readonly ISelector<ImmutableList<(SlotRef Slot, ImmutableList<IGlimpseImage> Icons)>> s_slotToWindowGroupIcons =
		CreateSelector(
			Slots,
			s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef.Id == y.WindowRef.Id && x.Icons == y.Icons),
			(slots, windows) =>
			{
				return windows.Select(window => (Slot: slots.Refs.First(s => s.ClassHintName == window.ClassHintName), Icons: window.Icons.ToImmutableList())).ToImmutableList();
			});

	private static readonly ISelector<ImmutableList<(SlotRef Slot, ImageViewModel Icon)>> s_slotToIcon = CreateSelector(
		Slots,
		s_slotToDesktopFile,
		s_slotToWindowGroupIcons,
		(slots, desktopFiles, windowGroups) =>
		{
			return slots.Refs
				.Select(slot =>
				{
					var desktopFile = desktopFiles.FirstOrDefault(f => f.Slot == slot).DesktopFile;
					var iconViewModel = new ImageViewModel() { IconNameOrPath = desktopFile?.IconName };

					if (string.IsNullOrEmpty(iconViewModel.IconNameOrPath))
					{
						var allIcons = windowGroups.FirstOrDefault(w => w.Slot == slot).Icons ?? ImmutableList<IGlimpseImage>.Empty;
						var biggestIcon = allIcons.Any() ? allIcons.MaxBy(i => i.Width) : null;
						iconViewModel.Image = biggestIcon;
					}

					return (Slot: slot, Icon: iconViewModel);
				})
				.ToImmutableList();
		},
		(x, y) => CollectionComparer.Sequence(x, y, (i, j) => i.Slot == j.Slot && i.Icon == j.Icon));

	private static readonly ISelector<ImmutableList<(SlotRef Slot, bool CanClose)>> s_slotToCanClose = CreateSelector(
		Slots,
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef.Id == y.WindowRef.Id && x.ClassHintName == y.ClassHintName),
		(slots, windows) =>
		{
			return slots.Refs.Select(slot => (Slot: slot, CanClose: windows.Count(w => w.ClassHintName == slot.ClassHintName) > 0)).ToImmutableList();
		},
		(x, y) => CollectionComparer.Sequence(x, y, (i, j) => i.Slot == j.Slot && i.CanClose == j.CanClose));

	private static readonly ISelector<ImmutableList<(SlotRef Slot, TaskbarGroupContextMenuViewModel ViewModel)>> s_contextMenu = CreateSelector(
		Slots,
		s_slotToCanClose,
		s_slotToDesktopFile,
		s_slotToIcon,
		(slots, canClose, desktopFiles, icons) =>
		{
			return slots.Refs.Select(slot =>
			{
				var desktopFile = desktopFiles.First(f => f.Slot == slot).DesktopFile;
				var icon = icons.First(s => s.Slot == slot).Icon;

				return (slot, new TaskbarGroupContextMenuViewModel
				{
					IsPinned = !string.IsNullOrEmpty(slot.PinnedDesktopFileId),
					DesktopFile = desktopFile,
					LaunchIcon = icon,
					CanClose = canClose.First(w => w.Slot == slot).CanClose,
					ActionIcons = desktopFile.Actions.ToDictionary(a => a.ActionName, a => icon)
				});
			}).ToImmutableList();
		},
		(x, y) => CollectionComparer.Sequence(x, y));

	private static readonly ISelector<ImmutableList<KeyValuePair<IWindowRef, ImageViewModel>>> s_windowToIcon = CreateSelector(
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef == y.WindowRef && x.Icons == y.Icons),
		(windows) => windows
			.Select(w => KeyValuePair.Create(w.WindowRef, new ImageViewModel() { Image = w.Icons.MaxBy(i => i.Width), IconNameOrPath = "" }))
			.ToImmutableList());

	private static readonly ISelector<ImmutableList<KeyValuePair<IWindowRef, ImageViewModel>>> s_windowToScreenshot = CreateSelector(
		Slots,
		XorgSelectors.Screenshots,
		s_slotToDesktopFile,
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef == y.WindowRef && x.Icons == y.Icons),
		(slots, screenshots, desktopFiles, windows) =>
		{
			return windows
				.Select(w =>
				{
					var slot = slots.Refs.FirstOrDefault(s => s.ClassHintName == w.ClassHintName);
					var desktopFile = desktopFiles.First(f => f.Slot == slot).DesktopFile;
					var lastScreenshot = screenshots.ById.GetValueOrDefault(w.Id);

					return KeyValuePair.Create(w.WindowRef, new ImageViewModel()
					{
						Image = lastScreenshot ?? w.Icons.MaxBy(i => i.Width),
						IconNameOrPath = desktopFile.IconName
					});
				})
				.ToImmutableList();
		});

	private static readonly ISelector<ImmutableList<(string ClassHintName, ImmutableList<WindowViewModel> ViewModels)>> s_windowViewModels = CreateSelector(
		s_windowToScreenshot,
		s_windowToIcon,
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.Title == y.Title && x.DemandsAttention == y.DemandsAttention && x.AllowActions == y.AllowActions && x.WindowRef == y.WindowRef && x.Icons == y.Icons),
		(screenshots, icons, windows) => windows
			.GroupBy(w => w.ClassHintName)
			.Select(g =>
			{
				return (ClassHintName: g.Key, g
					.Select(w => new WindowViewModel
					{
						Icon = icons.First(kv => kv.Key.Id == w.WindowRef.Id).Value,
						Title = w.Title,
						WindowRef = w.WindowRef,
						AllowedActions = w.AllowActions,
						DemandsAttention = w.DemandsAttention,
						Screenshot = screenshots.First(s => s.Key == w.WindowRef).Value
					})
					.ToImmutableList());
			})
			.ToImmutableList());

	public static readonly ISelector<TaskbarViewModel> ViewModel = CreateSelector(
		Slots,
		s_slotToDesktopFile,
		s_contextMenu,
		s_slotToIcon,
		s_windowViewModels,
		(slots, desktopFiles, contextMenuViewModels, slotsToIcon, windowViewModels) => new TaskbarViewModel
		{
			Groups = slots.Refs
				.Select(slot =>
				{
					var windowViewModelsForSlot = windowViewModels.FirstOrDefault(w => w.ClassHintName == slot.ClassHintName).ViewModels ?? ImmutableList<WindowViewModel>.Empty;

					return new SlotViewModel
					{
						SlotRef = slot,
						DesktopFile = desktopFiles.First(s => s.Slot == slot).DesktopFile,
						Icon = slotsToIcon.First(s => s.Slot == slot).Icon,
						Tasks = windowViewModelsForSlot,
						DemandsAttention = windowViewModelsForSlot.Any(w => w.DemandsAttention),
						ContextMenu = contextMenuViewModels.First(vm => vm.Slot == slot).ViewModel
					};
				})
				.ToImmutableList()
		});

	private static DesktopFile FindAppDesktopFileByName(IEnumerable<DesktopFile> desktopFiles, string classHintName)
	{
		return desktopFiles.FirstOrDefault(f => f.Name.Contains(classHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => f.FileName.Equals(classHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => f.StartupWmClass.Contains(classHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => f.Executable.Contains(classHintName, StringComparison.OrdinalIgnoreCase) && f.Executable.Length == f.CommandLine.Length)
			?? desktopFiles.FirstOrDefault(f => f.Executable.Contains(classHintName, StringComparison.OrdinalIgnoreCase));
	}
}
