using System.Collections.Immutable;
using Fluxor;
using Glimpse.Services.FreeDesktop;

namespace Glimpse.Components.StartMenu;

[FeatureState]
public record StartMenuState
{
	public ImmutableList<DesktopFile> PinnedDesktopFiles = ImmutableList<DesktopFile>.Empty;
	public string SearchText { get; init; } = "";
	public string PowerButtonCommand { get; init; } = "xfce4-session-logout";
	public string SettingsButtonCommand { get; init; } = "xfce4-settings-manager";
	public string UserSettingsCommand { get; init; } = "mugshot";

	public ImmutableDictionary<StartMenuChips, StartMenuAppFilteringChip> Chips { get; init; } =
		ImmutableDictionary<StartMenuChips, StartMenuAppFilteringChip>.Empty
			.Add(StartMenuChips.Pinned, new() { IsSelected = true, IsVisible = true })
			.Add(StartMenuChips.AllApps, new() { IsSelected = false, IsVisible = true })
			.Add(StartMenuChips.SearchResults, new() { IsSelected = false, IsVisible = false });

	public virtual bool Equals(StartMenuState other) => ReferenceEquals(this, other);
}

public enum StartMenuChips
{
	Pinned,
	AllApps,
	SearchResults
}

public record StartMenuAppFilteringChip
{
	public bool IsVisible { get; set; }
	public bool IsSelected { get; set; }
}

public class StartMenuReducer
{
	[ReducerMethod]
	public static StartMenuState ReduceUpdateUserSettingsCommandAction(StartMenuState state, UpdateUserSettingsCommandAction action)
	{
		return state with { UserSettingsCommand = action.Command };
	}

	[ReducerMethod]
	public static StartMenuState ReduceUpdateSettingsButtonCommandAction(StartMenuState state, UpdateSettingsButtonCommandAction action)
	{
		return state with { SettingsButtonCommand = action.Command };
	}

	[ReducerMethod]
	public static StartMenuState ReduceUpdateDesktopFilesAction(StartMenuState state, UpdatePowerButtonCommandAction action)
	{
		return state with { PowerButtonCommand = action.Command };
	}

	[ReducerMethod]
	public static StartMenuState ReduceAddStartMenuPinnedDesktopFileAction(StartMenuState state, AddStartMenuPinnedDesktopFileAction action)
	{
		return state with
		{
			PinnedDesktopFiles = state.PinnedDesktopFiles.Add(action.DesktopFile)
		};
	}

	[ReducerMethod]
	public static StartMenuState ReduceToggleStartMenuPinningAction(StartMenuState state, ToggleStartMenuPinningAction action)
	{
		var pinnedApps = state.PinnedDesktopFiles;
		var desktopFileToRemove = pinnedApps.FirstOrDefault(a => a.IniFile.FilePath == action.DesktopFile.IniFile.FilePath);

		if (desktopFileToRemove != null)
		{
			return state with { PinnedDesktopFiles = pinnedApps.Remove(desktopFileToRemove) };
		}

		return state with { PinnedDesktopFiles = pinnedApps.Add(action.DesktopFile) };
	}

	[ReducerMethod]
	public static StartMenuState ReduceUpdateStartMenuSearchTextAction(StartMenuState state, UpdateStartMenuSearchTextAction action)
	{
		var chips = state.Chips;

		if (string.IsNullOrEmpty(action.SearchText))
		{
			chips = chips.SetItem(StartMenuChips.Pinned, new StartMenuAppFilteringChip { IsSelected = true, IsVisible = true });
			chips = chips.SetItem(StartMenuChips.AllApps, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = true });
			chips = chips.SetItem(StartMenuChips.SearchResults, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = false });
		}
		else
		{
			chips = chips.SetItem(StartMenuChips.Pinned, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = true });
			chips = chips.SetItem(StartMenuChips.AllApps, new StartMenuAppFilteringChip { IsSelected = false, IsVisible = true });
			chips = chips.SetItem(StartMenuChips.SearchResults, new StartMenuAppFilteringChip { IsSelected = true, IsVisible = true });
		}

		return state with { SearchText = action.SearchText, Chips = chips };
	}

	[ReducerMethod]
	public static StartMenuState ReduceUpdatePinnedAppOrderingAction(StartMenuState state, UpdatePinnedAppOrderingAction action)
	{
		var pinnedAppToMove = state.PinnedDesktopFiles.First(f => f.IniFile.FilePath == action.DesktopFileKey);
		var newPinnedFiles = state.PinnedDesktopFiles.Remove(pinnedAppToMove).Insert(action.NewIndex, pinnedAppToMove);
		return state with { PinnedDesktopFiles = newPinnedFiles };
	}

	[ReducerMethod]
	public static StartMenuState ReduceUpdateAppFilteringChip(StartMenuState state, UpdateAppFilteringChip action)
	{
		var chips = state.Chips;
		chips = chips.SetItem(StartMenuChips.Pinned, chips[StartMenuChips.Pinned] with { IsSelected = action.Chip == StartMenuChips.Pinned });
		chips = chips.SetItem(StartMenuChips.AllApps, chips[StartMenuChips.AllApps] with { IsSelected = action.Chip == StartMenuChips.AllApps });
		chips = chips.SetItem(StartMenuChips.SearchResults, chips[StartMenuChips.SearchResults] with { IsSelected = action.Chip == StartMenuChips.SearchResults });
		return state with { Chips = chips };
	}
}

public class ToggleStartMenuPinningAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class UpdatePowerButtonCommandAction
{
	public string Command { get; init; }
}

public class UpdateSettingsButtonCommandAction
{
	public string Command { get; init; }
}

public class AddStartMenuPinnedDesktopFileAction
{
	public DesktopFile DesktopFile { get; init; }
}

public class UpdateStartMenuSearchTextAction
{
	public string SearchText { get; init; }
}

public class UpdateUserSettingsCommandAction
{
	public string Command { get; init; }
}

public class UpdatePinnedAppOrderingAction
{
	public string DesktopFileKey { get; set; }
	public int NewIndex { get; set; }
}

public class UpdateAppFilteringChip
{
	public StartMenuChips Chip { get; set; }
}
