using System.Collections.Immutable;
using Gdk;
using Glimpse.Extensions;
using Glimpse.Extensions.Redux.Selectors;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using static Glimpse.Extensions.Redux.Selectors.SelectorFactory;

namespace Glimpse.Components.Taskbar;

public static class TaskbarSelectors
{
	private static readonly ISelector<ImmutableList<WindowProperties>> s_windowPropertiesList = CreateSelector(
		RootStateSelectors.Windows,
		windows => windows.ById.Values.ToImmutableList());

	public static readonly ISelector<SlotReferences> Slots = CreateSelector(
		RootStateSelectors.DesktopFiles,
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef.Id == y.WindowRef.Id && x.ClassHintName == y.ClassHintName),
		RootStateSelectors.UserSortedSlots,
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
		RootStateSelectors.DesktopFiles,
		(slots, desktopFiles) =>
		{
			return slots.Refs.Select(slot => (
					Slots: slot,
					DesktopFile: desktopFiles.ContainsKey(slot.PinnedDesktopFileId) ? desktopFiles.ById[slot.PinnedDesktopFileId]
					: desktopFiles.ContainsKey(slot.DiscoveredDesktopFileId) ? desktopFiles.ById[slot.DiscoveredDesktopFileId]
					: new DesktopFile()))
				.ToImmutableList();
		},
		(x, y) => x.SequenceEqual(y, new FuncEqualityComparer<(SlotRef Slot, DesktopFile DesktopFile)>((s1, s2) => s1.Slot == s2.Slot && s1.DesktopFile.Id == s2.DesktopFile.Id)));

	private static readonly ISelector<ImmutableList<(SlotRef Slot, ImmutableList<Pixbuf> Icons)>> s_slotToWindowGroupIcons =
		CreateSelector(
			Slots,
			s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef.Id == y.WindowRef.Id && x.Icons == y.Icons),
			(slots, windows) =>
			{
				return windows.Select(window => (Slot: slots.Refs.First(s => s.ClassHintName == window.ClassHintName), Icons: window.Icons.ToImmutableList())).ToImmutableList();
			});

	private static readonly ISelector<ImmutableList<(SlotRef Slot, Pixbuf Icon)>> s_slotToIcon = CreateSelector(
		Slots,
		s_slotToDesktopFile,
		s_slotToWindowGroupIcons,
		RootStateSelectors.NamedIcons,
		(slots, desktopFiles, windowGroups, icons) =>
		{
			return slots.Refs
				.Select(slot =>
				{
					var allIcons = windowGroups.FirstOrDefault(w => w.Slot == slot).Icons ?? ImmutableList<Pixbuf>.Empty;
					var desktopFile = desktopFiles.FirstOrDefault(f => f.Slot == slot).DesktopFile;
					var biggestIcon = allIcons.Any() ? allIcons.MaxBy(i => i.Width) : null;
					icons.ById.TryGetValue(desktopFile.IconName, out var desktopFileIcon);
					return (Slot: slot, Icon: desktopFileIcon ?? biggestIcon ?? Assets.MissingImage);
				})
				.ToImmutableList();
		},
		(x, y) => x.SequenceEqual(y, new FuncEqualityComparer<(SlotRef Slot, Pixbuf Icon)>((s1, s2) => s1.Slot == s2.Slot && s1.Icon == s2.Icon)));

	private static readonly ISelector<ImmutableList<(SlotRef Slot, bool CanClose)>> s_slotToCanClose = CreateSelector(
		Slots,
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef.Id == y.WindowRef.Id && x.ClassHintName == y.ClassHintName),
		(slots, windows) =>
		{
			return slots.Refs.Select(slot => (slot, windows.Count(w => w.ClassHintName == slot.ClassHintName) > 0)).ToImmutableList();
		},
		(x, y) => x.SequenceEqual(y, new FuncEqualityComparer<(SlotRef Slot, bool CanClose)>((s1, s2) => s1.Slot == s2.Slot && s1.CanClose == s2.CanClose)));

	private static readonly ISelector<ImmutableList<(SlotRef Slot, TaskbarGroupContextMenuViewModel ViewModel)>> s_contextMenu = CreateSelector(
		Slots,
		s_slotToCanClose,
		s_slotToDesktopFile,
		s_slotToIcon,
		RootStateSelectors.NamedIcons,
		(slots, canClose, desktopFiles, icons, iconCache) =>
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
					ActionIcons = desktopFile.Actions.ToDictionary(
						a => a.ActionName,
						a => string.IsNullOrEmpty(a.IconName) ? icon
							: iconCache.ById.TryGetValue(a.IconName, out var i) ? i
							: Assets.MissingImage)
				});
			}).ToImmutableList();
		},
		(x, y) => x.SequenceEqual(y));

	private static readonly ISelector<ImmutableList<KeyValuePair<IWindowRef, Pixbuf>>> s_windowToIcon = CreateSelector(
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.WindowRef == y.WindowRef && x.Icons == y.Icons),
		(windows) => windows
			.Select(w => KeyValuePair.Create(w.WindowRef, w.Icons.Any() ? w.Icons.MaxBy(i => i.Width) : Assets.MissingImage))
			.ToImmutableList());

	private static readonly ISelector<ImmutableList<(string ClassHintName, ImmutableList<WindowViewModel> ViewModels)>> s_windowViewModels = CreateSelector(
		RootStateSelectors.Screenshots,
		s_windowToIcon,
		s_windowPropertiesList.WithSequenceComparer((x, y) => x.Title == y.Title && x.DemandsAttention == y.DemandsAttention && x.AllowActions == y.AllowActions && x.WindowRef == y.WindowRef && x.Icons == y.Icons),
		(screenshots, icons, windows) => windows
			.GroupBy(w => w.ClassHintName)
			.Select(g => (ClassHintName: g.Key, g
				.Select(w => new WindowViewModel
				{
					Icon = icons.First(kv => kv.Key.Id == w.WindowRef.Id).Value,
					Title = w.Title,
					WindowRef = w.WindowRef,
					AllowedActions = w.AllowActions,
					DemandsAttention = w.DemandsAttention,
					Screenshot = screenshots.ById.FirstOrDefault(s => s.Key == w.WindowRef.Id).Value ?? w.DefaultScreenshot
				})
				.ToImmutableList()))
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
			?? desktopFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.IniFile.FilePath).Equals(classHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => f.StartupWmClass.Contains(classHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => f.Exec.Executable.Contains(classHintName, StringComparison.OrdinalIgnoreCase) && f.Exec.Arguments.Length == 0)
			?? desktopFiles.FirstOrDefault(f => f.Exec.Executable.Contains(classHintName, StringComparison.OrdinalIgnoreCase));
	}
}
