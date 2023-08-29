using Gdk;
using Gtk;
using GtkNetPanel.Services.SystemTray;
using Menu = Gtk.Menu;
using MenuItem = Gtk.MenuItem;

namespace GtkNetPanel.Components.Shared.ContextMenu;

public static class DbusContextMenuHelpers
{
	public static DbusSystemTrayMenuItem GetDbusMenuItem(this MenuItem menuItem)
	{
		return menuItem.Data["DbusMenuItem"] as DbusSystemTrayMenuItem;
	}
	public static LinkedList<MenuItem> GetAllMenuItems(Menu rootMenu)
	{
		var result = new LinkedList<MenuItem>();
		var queue = new Queue<Menu>();
		queue.Enqueue(rootMenu);

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			current.Children.OfType<MenuItem>().ToList().ForEach(i => result.AddLast(i));
			current.Children.OfType<Menu>().ToList().ForEach(i => queue.Enqueue(i));
		}

		return result;
	}
	public static void PopulateMenu(Menu rootMenu, DbusSystemTrayMenuItem rootItem)
	{
		foreach (var childMenuItem in rootItem.Children) PopulateMenuInternal(rootMenu, childMenuItem);
		rootMenu.ShowAll();
	}

	private static void PopulateMenuInternal(Menu parentMenu, DbusSystemTrayMenuItem dbusSystemTrayMenuItem)
	{
		if (dbusSystemTrayMenuItem.Type == "separator")
		{
			parentMenu.Add(new SeparatorMenuItem());
		}
		else
		{
			var menuItem = CreateMenuItem(dbusSystemTrayMenuItem);
			parentMenu.Add(menuItem);

			if (dbusSystemTrayMenuItem.Children.Any())
			{
				var childMenu = new Menu();
				menuItem.Submenu = childMenu;
				foreach (var childItem in dbusSystemTrayMenuItem.Children) PopulateMenuInternal(childMenu, childItem);
			}
		}
	}

	private static MenuItem CreateMenuItem(DbusSystemTrayMenuItem item)
	{
		var box = new Box(Orientation.Horizontal, 10);

		if (item.IconData != null)
		{
			var loader = new PixbufLoader(item.IconData);
			box.PackStart(new Image(loader.Pixbuf.Copy()), false, false, 0);
		}
		else if (!string.IsNullOrEmpty(item.IconName))
		{
			box.PackStart(Image.NewFromIconName(item.IconName, IconSize.Menu), false, false, 0);
		}

		box.PackStart(new Label(item.Label), false, false, 0);

		var menuItem = new MenuItem();
		menuItem.Data.Add("DbusMenuItem", item);
		menuItem.Add(box);
		return menuItem;
	}
}
