using System.Collections.Immutable;
using Fluxor.Selectors;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;

namespace Glimpse.Components.Taskbar;

public static class TaskbarSelectors
{
	public static readonly ISelector<SlotReferences> Slots = SelectorFactory.CreateSelector(
		RootStateSelectors.TaskbarPinnedLaunchers,
		RootStateSelectors.DesktopFiles,
		RootStateSelectors.Windows,
		(pinnedLaunchers, desktopFiles, windows) =>
		{
			var result = ImmutableList<SlotRef>.Empty;
			var groups = windows.ById.GroupBy(kv => kv.Value.ClassHintName).ToList();
			var pinnedDesktopFileRefs = pinnedLaunchers.Select(l => desktopFiles.ContainsKey(l) ? desktopFiles.ById[l] : null).Where(f => f != null).ToList();

			foreach (var pinned in pinnedDesktopFileRefs)
			{
				result = result.Add(new SlotRef { DesktopFileId = pinned.Id });
			}

			var pinnedDesktopFiles = result.Select(r => desktopFiles.ById[r.DesktopFileId]).ToList();

			foreach (var g in groups)
			{
				var matchingDesktopFile = FindAppDesktopFileByName(pinnedDesktopFiles, g.First().Value)
					?? FindAppDesktopFileByName(desktopFiles.ById.Values, g.First().Value);

				if (matchingDesktopFile != null)
				{
					var existingSlot = result.FirstOrDefault(s => s.DesktopFileId == matchingDesktopFile.Id);

					if (existingSlot != null && string.IsNullOrEmpty(existingSlot.ClassHintName))
					{
						result = result.Replace(existingSlot, existingSlot with { ClassHintName = g.Key });
					}
					else if (existingSlot == null)
					{
						result = result.Add(new SlotRef() { ClassHintName = g.Key });
					}
				}
				else
				{
					result = result.Add(new SlotRef { ClassHintName = g.Key });
				}
			}

			return new SlotReferences { Refs = result };
		});

	public static readonly ISelector<SlotReferences> SortedSlots = SelectorFactory.CreateSelector(
		Slots,
		RootStateSelectors.TaskbarSlotCollection,
		(slotCollection, ordering) =>
		{
			var updatedSlots = slotCollection.Refs;
			var results = ImmutableList<SlotRef>.Empty;

			foreach (var o in ordering.Refs)
			{
				if (!string.IsNullOrEmpty(o.DesktopFileId) && updatedSlots.FirstOrDefault(s => s.DesktopFileId == o.DesktopFileId) is { } m1)
				{
					results = results.Add(m1);
				}
				else if (!string.IsNullOrEmpty(o.ClassHintName) && updatedSlots.FirstOrDefault(s => s.ClassHintName == o.ClassHintName) is { } m2)
				{
					results = results.Add(m2);
				}
			}

			foreach (var s in updatedSlots)
			{
				if (!string.IsNullOrEmpty(s.DesktopFileId) && results.FirstOrDefault(o => o.DesktopFileId == s.DesktopFileId) is null)
				{
					results = results.Add(s);
				}
				else if (!string.IsNullOrEmpty(s.ClassHintName) && results.FirstOrDefault(o => o.ClassHintName == s.ClassHintName) is null)
				{
					results = results.Add(s);
				}
			}

			return new SlotReferences { Refs = results };
		});

	public static readonly ISelector<TaskbarViewModel> ViewModel = SelectorFactory.CreateSelector(
		SortedSlots,
		RootStateSelectors.Windows,
		RootStateSelectors.NamedIcons,
		RootStateSelectors.DesktopFiles,
		RootStateSelectors.Screenshots,
		(slots, windows, icons, desktopFiles, screenshots) =>
		{
			Console.WriteLine(DateTime.Now.TimeOfDay);
			return new TaskbarViewModel
			{
				Groups = slots.Refs.Select(slot =>
				{
					var windowGroup = windows.ById.Values.Where(v => v.ClassHintName == slot.ClassHintName).ToList();
					var desktopFile = desktopFiles.ContainsKey(slot.DesktopFileId) ? desktopFiles.ById[slot.DesktopFileId] : new DesktopFile();
					var allIcons = windowGroup.SelectMany(w => w.Icons).ToList();
					var biggestIcon = allIcons.Any() ? allIcons.MaxBy(i => i.Width) : null;
					icons.ById.TryGetValue(desktopFile.IconName, out var desktopFileIcon);
					var icon = desktopFileIcon ?? biggestIcon?.ToPixbuf() ?? Assets.MissingImage;

					return new TaskbarGroupViewModel
					{
						SlotRef = slot,
						DesktopFile = desktopFile,
						Icon = icon,
						Tasks = windowGroup.Select(w => new SlotWindowViewModel
						{
							Icon = w.Icons.Any() ? w.Icons.MaxBy(i => i.Width)?.ToPixbuf() : Assets.MissingImage,
							Title = w.Title,
							WindowRef = w.WindowRef,
							AllowedActions = w.AllowActions,
							Screenshot = screenshots.ById.FirstOrDefault(s => s.Key == w.WindowRef.Id).Value?.ToPixbuf()
								?? w.Icons?.MaxBy(i => i.Width)?.ToPixbuf()
								?? Assets.MissingImage
						}).ToImmutableList(),
						DemandsAttention = windowGroup.Any(w => w.DemandsAttention),
						ContextMenu = new TaskbarGroupContextMenuViewModel
						{
							IsPinned = !string.IsNullOrEmpty(slot.DesktopFileId),
							DesktopFile = desktopFile,
							LaunchIcon = icon,
							CanClose = windowGroup.Any(), // Check window actions.  Careful not to cause it to run a bunch here
							ActionIcons = desktopFile.Actions.ToDictionary(
								a => a.ActionName,
								a => string.IsNullOrEmpty(a.IconName) ? icon
									: icons.ById.TryGetValue(a.IconName, out var i) ? i
									: Assets.MissingImage)
						}
					};
				}).ToImmutableList()
			};
		});

	private static DesktopFile FindAppDesktopFileByName(IEnumerable<DesktopFile> desktopFiles, WindowProperties windowProperties)
	{
		return desktopFiles.FirstOrDefault(f => f.Name.Contains(windowProperties.ClassHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.IniFile.FilePath).Equals(windowProperties.ClassHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => f.StartupWmClass.Contains(windowProperties.ClassHintName, StringComparison.OrdinalIgnoreCase))
			?? desktopFiles.FirstOrDefault(f => f.Exec.Executable.Contains(windowProperties.ClassHintName, StringComparison.OrdinalIgnoreCase) && f.Exec.Arguments.Length == 0)
			?? desktopFiles.FirstOrDefault(f => f.Exec.Executable.Contains(windowProperties.ClassHintName, StringComparison.OrdinalIgnoreCase));
	}
}
