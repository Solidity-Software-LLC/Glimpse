using System.Reactive.Linq;
using Fluxor;
using Gtk;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.Tray;

public class SystemTrayBox : Box
{
	private readonly IState<SystemTrayState> _trayState;
	private readonly IDispatcher _dispatcher;
	private readonly Dictionary<string, SystemTrayIcon> _icons = new();

	public SystemTrayBox(IState<SystemTrayState> trayState, IDispatcher dispatcher) : base(Orientation.Horizontal, 3)
	{
		_trayState = trayState;
		_dispatcher = dispatcher;
		trayState.ToObservable().Subscribe(s =>
		{
			OnItemsChanged(s.Items);
		});
	}

	private void OnItemsChanged(IDictionary<string, SystemTrayItemState> items)
	{
		foreach (var i in items)
		{
			if (!_icons.ContainsKey(i.Key))
			{
				AddSystemTrayIcon(i);
			}
		}

		foreach (var iconServiceName in _icons.Keys)
		{
			if (!items.ContainsKey(iconServiceName))
			{
				RemoveSystemTrayIcon(iconServiceName);
			}
		}

		ShowAll();
	}

	private void RemoveSystemTrayIcon(string iconServiceName)
	{
		var icon = _icons[iconServiceName];
		_icons.Remove(iconServiceName);
		Remove(icon);
	}

	private void AddSystemTrayIcon(KeyValuePair<string, SystemTrayItemState> kv)
	{
		var rootMenuObservable = _trayState.ToObservable().TakeUntil(s => !s.Items.ContainsKey(kv.Key)).Select(s => s.Items[kv.Key]);
		var systemTrayIcon = new SystemTrayIcon(rootMenuObservable);

		systemTrayIcon.MenuItemActivated.Subscribe(id =>
		{
			_dispatcher.Dispatch(new ActivateMenuItemAction() { DbusObjectDescription = kv.Value.DbusMenuDescription, MenuItemId = id });
		});

		systemTrayIcon.ApplicationActivated.Subscribe(t =>
		{
			_dispatcher.Dispatch(new ActivateApplicationAction() { DbusObjectDescription = kv.Value.StatusNotifierItemDescription, X = t.Item1, Y = t.Item2 });
		});

		PackStart(systemTrayIcon, false, false, 3);
		_icons.Add(kv.Key, systemTrayIcon);
	}
}
