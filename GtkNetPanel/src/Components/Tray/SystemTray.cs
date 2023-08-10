using System.Reactive.Linq;
using Fluxor;
using Gtk;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.Tray;

public class SystemTray : Box
{
	private readonly IState<TrayState> _trayState;
	private readonly Dictionary<string, SystemTrayIcon> _icons = new();

	public SystemTray(IState<TrayState> trayState) : base(Orientation.Horizontal, 3)
	{
		_trayState = trayState;
		trayState.ToObservable().Subscribe(s =>
		{
			OnItemsChanged(s.Items);
		});
	}

	private void OnItemsChanged(IDictionary<string, TrayItemState> items)
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

	private void AddSystemTrayIcon(KeyValuePair<string, TrayItemState> kv)
	{
		var rootMenuObservable = _trayState.ToObservable().TakeUntil(s => !s.Items.ContainsKey(kv.Key)).Select(s => s.Items[kv.Key]);
		var systemTrayIcon = new SystemTrayIcon(rootMenuObservable);
		PackStart(systemTrayIcon, false, false, 3);
		_icons.Add(kv.Key, systemTrayIcon);
	}
}
