using System.Collections.Immutable;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Redux.Selectors;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using static Glimpse.Extensions.Redux.Selectors.SelectorFactory;
namespace Glimpse.Components.Taskbar;

public static class TaskbarSelectors
{
	public static readonly ISelector<SlotReferences> Slots = CreateSelector(
		RootStateSelectors.DesktopFiles,
		RootStateSelectors.Windows,
		RootStateSelectors.UserSortedSlots,
		(desktopFiles, windows, userSortedSlotCollection) =>
		{
			var result = ImmutableList<SlotRef>.Empty.AddRange(userSortedSlotCollection.Refs);

			var previouslyFoundDesktopFiles = userSortedSlotCollection.Refs
				.Select(l => desktopFiles.ContainsKey(l.PinnedDesktopFileId) ? desktopFiles.ById[l.PinnedDesktopFileId] : null)
				.Where(f => f != null)
				.ToList();

			foreach (var window in windows.ById.Values.OrderBy(w => w.CreationDate))
			{
				if (result.Any(s => s.ClassHintName == window.ClassHintName)) continue;
				var desktopFile = FindAppDesktopFileByName(previouslyFoundDesktopFiles, window) ?? FindAppDesktopFileByName(desktopFiles.ById.Values, window);
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

	public static readonly ISelector<TaskbarViewModel> ViewModel = CreateSelector(
		Slots,
		RootStateSelectors.Windows,
		RootStateSelectors.NamedIcons,
		RootStateSelectors.DesktopFiles,
		RootStateSelectors.Screenshots,
		(slots, windows, icons, desktopFiles, screenshots) =>
		{
			return new TaskbarViewModel
			{
				Groups = slots.Refs.Select(slot =>
				{
					var windowGroup = windows.ById.Values.Where(v => v.ClassHintName == slot.ClassHintName).ToList();
					var desktopFile = desktopFiles.ContainsKey(slot.PinnedDesktopFileId) ? desktopFiles.ById[slot.PinnedDesktopFileId]
						: desktopFiles.ContainsKey(slot.DiscoveredDesktopFileId) ? desktopFiles.ById[slot.DiscoveredDesktopFileId]
						: new DesktopFile();
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
							IsPinned = !string.IsNullOrEmpty(slot.PinnedDesktopFileId),
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
