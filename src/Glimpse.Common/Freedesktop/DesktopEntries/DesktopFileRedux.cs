using System.Collections.Immutable;
using Glimpse.Redux;
using Glimpse.Redux.Reducers;
using Glimpse.Redux.Selectors;

namespace Glimpse.Freedesktop.DesktopEntries;

public class DesktopFileSelectors
{
	public static readonly ISelector<DataTable<string, DesktopFile>> DesktopFiles = SelectorFactory.CreateFeatureSelector<DataTable<string, DesktopFile>>();
	public static readonly ISelector<ImmutableList<DesktopFile>> AllDesktopFiles =
		SelectorFactory.CreateSelector(DesktopFiles, s => s.ById.Values
			.OrderBy(f => f.Name)
			.ToImmutableList());
}

public class UpdateDesktopFilesAction
{
	public ImmutableList<DesktopFile> DesktopFiles { get; init; }
}

public class DesktopFileReducers
{
	public static readonly FeatureReducerCollection AllReducers =
	[
		FeatureReducer.Build(new DataTable<string, DesktopFile>())
			.On<UpdateDesktopFilesAction>((s, a) => s.UpsertMany(a.DesktopFiles))
	];
}
