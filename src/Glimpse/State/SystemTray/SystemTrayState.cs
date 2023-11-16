using System.Collections.Immutable;
using Fluxor;
using Glimpse.Services.DBus;
using Glimpse.Services.DBus.Introspection;
using Glimpse.Services.SystemTray;

namespace Glimpse.State.SystemTray;

[FeatureState]
public class SystemTrayState
{
	public ImmutableDictionary<string, SystemTrayItemState> Items { get; init; }= ImmutableDictionary<string, SystemTrayItemState>.Empty;
}

public class SystemTrayItemState
{
	public StatusNotifierItemProperties Properties { get; init; }
	public DbusObjectDescription StatusNotifierItemDescription { get; init; }
	public DbusObjectDescription DbusMenuDescription { get; init; }
	public DbusMenuItem RootMenuItem { get; init; }

	public SystemTrayItemState()
	{

	}

	public SystemTrayItemState(SystemTrayItemState other)
	{
		Properties = other.Properties;
		StatusNotifierItemDescription = other.StatusNotifierItemDescription;
		DbusMenuDescription = other.DbusMenuDescription;
		RootMenuItem = other.RootMenuItem;
	}
}

public static class SystemTrayItemStateExtensions
{
	public static string GetServiceName(this SystemTrayItemState itemState) => itemState.StatusNotifierItemDescription.ServiceName;
}

public class AddBulkTrayItemsAction
{
	public IEnumerable<SystemTrayItemState> Items { get; init; }
}

public class AddTrayItemAction
{
	public SystemTrayItemState ItemState { get; init; }
}

public class RemoveTrayItemAction
{
	public string ServiceName { get; init; }
}

public class UpdateMenuLayoutAction
{
	public string ServiceName { get; init; }
	public DbusMenuItem RootMenuItem { get; init; }
}

public class ActivateApplicationAction
{
	public DbusObjectDescription DbusObjectDescription { get; init; }
	public int X { get; init; }
	public int Y { get; init; }
}

public class ActivateMenuItemAction
{
	public DbusObjectDescription DbusObjectDescription { get; init; }
	public int MenuItemId { get; init; }
}

public class UpdateStatusNotifierItemPropertiesAction
{
	public string ServiceName { get; init; }
	public StatusNotifierItemProperties Properties { get; init; }
}

public static class SystemTrayItemStateReducers
{
	[ReducerMethod]
	public static SystemTrayState ReduceUpdateStatusNotifierItemPropertiesAction(SystemTrayState state, UpdateStatusNotifierItemPropertiesAction action)
	{
		if (state.Items.TryGetValue(action.ServiceName, out var currentItem))
		{
			return new() { Items = state.Items.SetItem(action.ServiceName, new SystemTrayItemState(currentItem) { Properties = action.Properties }) };
		}

		return state;
	}

	[ReducerMethod]
	public static SystemTrayState ReduceUpdateMenuLayoutAction(SystemTrayState state, UpdateMenuLayoutAction action)
	{
		if (state.Items.TryGetValue(action.ServiceName, out var currentItem))
		{
			return new() { Items = state.Items.SetItem(action.ServiceName, new SystemTrayItemState(currentItem) { RootMenuItem = action.RootMenuItem }) };
		}

		return state;
	}

	[ReducerMethod]
	public static SystemTrayState ReduceAddBulkTrayItemsAction(SystemTrayState state, AddBulkTrayItemsAction action)
	{
		var newItemList = new LinkedList<SystemTrayItemState>();

		foreach (var item in action.Items)
		{
			if (!state.Items.ContainsKey(item.GetServiceName()))
			{
				newItemList.AddLast(item);
			}
		}

		// Add existing items too

		return new SystemTrayState()
		{
			Items = newItemList
				.DistinctBy(i => i.StatusNotifierItemDescription.ServiceName)
				.ToImmutableDictionary(i => i.StatusNotifierItemDescription.ServiceName, i => i)
		};
	}

	[ReducerMethod]
	public static SystemTrayState ReduceAddTrayItemAction(SystemTrayState state, AddTrayItemAction action)
	{
		if (!state.Items.ContainsKey(action.ItemState.GetServiceName()))
		{
			return new() { Items = state.Items.Add(action.ItemState.GetServiceName(), action.ItemState) };

		}

		return state;
	}

	[ReducerMethod]
	public static SystemTrayState ReduceRemoveTrayItemAction(SystemTrayState state, RemoveTrayItemAction action)
	{
		if (state.Items.ContainsKey(action.ServiceName))
		{
			return new() { Items = state.Items.Remove(action.ServiceName) };
		}

		return state;
	}
}

public class SystemTrayItemStateEffects(DBusSystemTrayService systemTrayService)
{
	[EffectMethod]
	public async Task HandleActivateApplicationAction(ActivateApplicationAction action, IDispatcher dispatcher)
	{
		await systemTrayService.ActivateSystemTrayItemAsync(action.DbusObjectDescription, action.X, action.Y);
	}

	[EffectMethod]
	public async Task HandleActivateMenuItemAction(ActivateMenuItemAction action, IDispatcher dispatcher)
	{
		await systemTrayService.ClickedItem(action.DbusObjectDescription, action.MenuItemId);
	}
}
