using System.Collections.Immutable;
using Fluxor;
using GtkNetPanel.Services.DBus;
using GtkNetPanel.Services.DBus.Introspection;
using GtkNetPanel.Services.DBus.Menu;
using GtkNetPanel.Services.DBus.StatusNotifierItem;
using GtkNetPanel.Services.SystemTray;

namespace GtkNetPanel.State;

[FeatureState]
public class SystemTrayState
{
	public ImmutableDictionary<string, SystemTrayItemState> Items = ImmutableDictionary<string, SystemTrayItemState>.Empty;
}

public class SystemTrayItemState
{
	public StatusNotifierItemProperties Properties { get; set; }
	public DbusObjectDescription StatusNotifierItemDescription { get; set; }
	public DbusObjectDescription DbusMenuDescription { get; set; }
	public DbusSystemTrayMenuItem RootSystemTrayMenuItem { get; set; }

	public SystemTrayItemState()
	{

	}

	public SystemTrayItemState(SystemTrayItemState other)
	{
		Properties = other.Properties;
		StatusNotifierItemDescription = other.StatusNotifierItemDescription;
		DbusMenuDescription = other.DbusMenuDescription;
		RootSystemTrayMenuItem = other.RootSystemTrayMenuItem;
	}
}

public static class SystemTrayItemStateExtensions
{
	public static string GetServiceName(this SystemTrayItemState itemState) => itemState.StatusNotifierItemDescription.ServiceName;
}

public class AddBulkTrayItemsAction
{
	public IEnumerable<SystemTrayItemState> Items { get; set; }
}

public class AddTrayItemAction
{
	public SystemTrayItemState ItemState { get; set; }
}

public class RemoveTrayItemAction
{
	public string ServiceName { get; set; }
}

public class UpdateMenuLayoutAction
{
	public string ServiceName { get; set; }
	public DbusSystemTrayMenuItem RootMenuItem { get; set; }
}

public class ActivateApplicationAction
{
	public DbusObjectDescription DbusObjectDescription { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
}

public class ActivateMenuItemAction
{
	public DbusObjectDescription DbusObjectDescription { get; set; }
	public int MenuItemId { get; set; }
}

public class UpdateStatusNotifierItemPropertiesAction
{
	public string ServiceName { get; set; }
	public StatusNotifierItemProperties Properties { get; set; }
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
			return new() { Items = state.Items.SetItem(action.ServiceName, new SystemTrayItemState(currentItem) { RootSystemTrayMenuItem = action.RootMenuItem }) };
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

public class SystemTrayItemStateEffects
{
	private readonly DBusSystemTrayService _systemTrayService;

	public SystemTrayItemStateEffects(DBusSystemTrayService systemTrayService)
	{
		_systemTrayService = systemTrayService;
	}

	[EffectMethod]
	public async Task HandleActivateApplicationAction(ActivateApplicationAction action, IDispatcher dispatcher)
	{
		await _systemTrayService.ActivateSystemTrayItemAsync(action.DbusObjectDescription, action.X, action.Y);
	}

	[EffectMethod]
	public async Task HandleActivateMenuItemAction(ActivateMenuItemAction action, IDispatcher dispatcher)
	{
		await _systemTrayService.ClickedItem(action.DbusObjectDescription, action.MenuItemId);
	}
}
