using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.DBus.Menu;
using Menu = Gtk.Menu;
using MenuItem = Gtk.MenuItem;

namespace GtkNetPanel.Components.ContextMenu;

public static class DBusMenuFactory
{
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
	public static void Create(Menu rootMenu, DbusMenuItem rootItem)
	{
		foreach (var childMenuItem in rootItem.Children) PopulateMenu(rootMenu, childMenuItem);
		rootMenu.ShowAll();
	}

	private static void PopulateMenu(Menu parentMenu, DbusMenuItem dbusMenuItem)
	{
		if (dbusMenuItem.Type == "separator")
		{
			parentMenu.Add(new SeparatorMenuItem());
		}
		else
		{
			var menuItem = CreateMenuItem(dbusMenuItem);
			parentMenu.Add(menuItem);

			if (dbusMenuItem.Children.Any())
			{
				var childMenu = new Menu();
				menuItem.Submenu = childMenu;
				foreach (var childItem in dbusMenuItem.Children) PopulateMenu(childMenu, childItem);
			}
		}
	}

	private static MenuItem CreateMenuItem(DbusMenuItem item)
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
