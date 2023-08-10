using System.Collections.Immutable;
using Fluxor;
using GtkNetPanel.Services.DBus.Menu;
using GtkNetPanel.Services.DBus.StatusNotifierItem;

namespace GtkNetPanel.State;

[FeatureState]
public class TrayState
{
	public ImmutableDictionary<string, TrayItemState> Items = ImmutableDictionary<string, TrayItemState>.Empty;
}

public class TrayItemState
{
	public DbusStatusNotifierItem Status { get; set; }
	public DbusMenuItem RootMenuItem { get; set; }
}

public static class TrayItemExtensions
{
	public static string GetServiceName(this TrayItemState itemState)
	{
		return itemState.Status.Object.ServiceName;
	}
}

public class AddBulkTrayItemsAction
{
	public IEnumerable<TrayItemState> Items { get; set; }
}

public class AddTrayItemAction
{
	public TrayItemState ItemState { get; set; }
}

public class RemoveTrayItemAction
{
	public string ServiceName { get; set; }
}

public class AddBulkTrayItemsActionReducer : Reducer<TrayState, AddBulkTrayItemsAction>
{
	public override TrayState Reduce(TrayState state, AddBulkTrayItemsAction action)
	{
		var newItemList = new LinkedList<TrayItemState>();

		foreach (var item in action.Items)
		{
			if (!state.Items.ContainsKey(item.Status.Object.ServiceName))
			{
				newItemList.AddLast(item);
			}
		}

		return new TrayState()
		{
			Items = newItemList
				.DistinctBy(i => i.Status.Object.ServiceName)
				.ToImmutableDictionary(i => i.Status.Object.ServiceName, i => i)
		};
	}
}

public class AddTrayItemActionReducer : Reducer<TrayState, AddTrayItemAction>
{
	public override TrayState Reduce(TrayState state, AddTrayItemAction action)
	{
		if (!state.Items.ContainsKey(action.ItemState.Status.Object.ServiceName))
		{
			return new() { Items = state.Items.Add(action.ItemState.Status.Object.ServiceName, action.ItemState) };

		}

		return state;
	}
}

public class RemoveTrayItemActionReducer : Reducer<TrayState, RemoveTrayItemAction>
{
	public override TrayState Reduce(TrayState state, RemoveTrayItemAction action)
	{
		if (state.Items.ContainsKey(action.ServiceName))
		{
			return new() { Items = state.Items.Remove(action.ServiceName) };
		}

		return state;
	}
}
