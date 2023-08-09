using Gdk;
using Gtk;
using GtkNetPanel.Tray;

namespace GtkNetPanel;

// Hover and click effects

public class SystemTrayIcon : EventBox
{
	private readonly StatusNotifierItem _statusNotifierItem;

	public SystemTrayIcon(StatusNotifierItem statusNotifierItem, Image image)
	{
		_statusNotifierItem = statusNotifierItem;
		HasTooltip = true;
		TooltipText = _statusNotifierItem.Properties.Category;

		Add(image);
		AddEvents((int)EventMask.ButtonReleaseMask);

		var popup = new Menu();
		PopulateMenu(popup);

		var helper = new ContextMenuHelper();
		helper.AttachToWidget(this);

		helper.ContextMenu += (_, _) =>
		{
			popup.ShowAll();
			popup.Popup();
		};

		ButtonReleaseEvent += async (o, args) =>
		{
			if (args.Event.Button != 1) return;

			if (_statusNotifierItem.Object.InterfaceHasMethod(IStatusNotifierItem.DbusInterfaceName, "Activate"))
			{
				await DBus.ActivateSystemTrayItemAsync(_statusNotifierItem, (int)args.Event.XRoot, (int)args.Event.YRoot);
			}
			else
			{
				popup.ShowAll();
				popup.Popup();
			}
		};
	}

	private async Task PopulateMenu(Menu popup)
	{
		var result = await DBus.GetMenuItems(_statusNotifierItem);
		var lastWasSeparator = false;

		foreach (var item in result.Children)
		{
			if (item.Type == "separator")
			{
				if (lastWasSeparator) continue;
				lastWasSeparator = true;
				popup.Add(new SeparatorMenuItem());
			}
			else
			{
				popup.Add(CreateMenuItem(item));
				lastWasSeparator = false;
			}
		}
	}

	private MenuItem CreateMenuItem(DbusMenuItem item)
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
		menuItem.Add(box);
		menuItem.Activated += (_, _) => DBus.ClickedItem(_statusNotifierItem, item.Id);
		return menuItem;
	}
}
