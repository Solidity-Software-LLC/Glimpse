using System.Collections.Immutable;
using Gdk;
using Gtk;
using GtkNetPanel.DBus.StatusNotifierItem;
using GtkNetPanel.DBus.StatusNotifierWatcher;

namespace GtkNetPanel.Components.Tray;

public class SystemTray : HBox
{
	private readonly StatusNotifierWatcherService _statusNotifierWatcherService = new();
	private readonly Dictionary<string, SystemTrayIcon> _icons = new();
	private readonly object _lock = new();

	private void OnItemsChanged(ImmutableList<DbusStatusNotifierItem> items)
	{
		lock (_lock)
		{
			foreach (var i in items)
			{
				if (!_icons.ContainsKey(i.Object.ServiceName))
				{
					AddSystemTrayIcon(i);
				}
			}

			foreach (var iconServiceName in _icons.Keys)
			{
				if (items.All(i => i.Object.ServiceName != iconServiceName))
				{
					RemoveSystemTrayIcon(iconServiceName);
				}
			}

			ShowAll();
		}
	}

	private void RemoveSystemTrayIcon(string iconServiceName)
	{
		var icon = _icons[iconServiceName];
		_icons.Remove(iconServiceName);
		Remove(icon);
	}

	private void AddSystemTrayIcon(DbusStatusNotifierItem item)
	{
		var systemTrayIcon = new SystemTrayIcon(item);
		PackStart(systemTrayIcon, false, false, 3);
		_icons.Add(item.Object.ServiceName, systemTrayIcon);
	}

	protected override void OnShown()
	{
		Task.Run(async () =>
		{
			try
			{
				await _statusNotifierWatcherService.LoadTrayItems();
				_statusNotifierWatcherService.StatusNotifierItems.Subscribe(OnItemsChanged);
				_statusNotifierWatcherService.Connect();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		});

		base.OnShown();
	}
}
