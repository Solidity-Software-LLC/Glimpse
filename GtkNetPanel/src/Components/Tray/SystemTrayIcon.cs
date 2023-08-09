using Gdk;
using Gtk;
using GtkNetPanel.Components.ContextMenu;
using GtkNetPanel.DBus.Menu;
using GtkNetPanel.DBus.StatusNotifierItem;

namespace GtkNetPanel.Components.Tray;

// Hover and click effects

public class SystemTrayIcon : EventBox
{
	private readonly DbusStatusNotifierItem _dbusStatusNotifierItem;
	private Menu _contextMenu;

	public SystemTrayIcon(DbusStatusNotifierItem dbusStatusNotifierItem)
	{
		var image = LoadImage(dbusStatusNotifierItem.Properties);

		_dbusStatusNotifierItem = dbusStatusNotifierItem;
		HasTooltip = true;
		TooltipText = _dbusStatusNotifierItem.Properties.Category;

		Add(image);
		AddEvents((int)EventMask.ButtonReleaseMask);

		var popup = new Menu();

		DBus.DBus.GetMenuItems(_dbusStatusNotifierItem).ContinueWith(result =>
		{
			DBusMenuFactory.Create(popup, result.Result);

			var allMenuItems = DBusMenuFactory.GetAllMenuItems(popup);

			foreach (var i in allMenuItems) i.Activated += (sender, args) => DBus.DBus.ClickedItem(_dbusStatusNotifierItem, (i.Data["DbusMenuItem"] as DbusMenuItem).Id);
		});

		var helper = new ContextMenuHelper();
		helper.AttachToWidget(this);

		helper.ContextMenu += (_, _) =>
		{
			if (popup.Children.Any())
			{
				popup.Popup();
			}
		};

		ButtonReleaseEvent += async (o, args) =>
		{
			if (args.Event.Button != 1) return;

			if (_dbusStatusNotifierItem.Object.InterfaceHasMethod(IStatusNotifierItem.DbusInterfaceName, "Activate"))
			{
				await DBus.DBus.ActivateSystemTrayItemAsync(_dbusStatusNotifierItem, (int)args.Event.XRoot, (int)args.Event.YRoot);
			}
			else
			{
				popup.Popup();
			}
		};
	}

	private byte[] ConvertArgbToRgba(byte[] data, int width, int height)
	{
		var newArray = new byte[data.Length];
		Array.Copy(data, newArray, data.Length);

		for (var i = 0; i < 4 * width * height; i += 4)
		{
			var alpha = newArray[i];
			newArray[i] = newArray[i + 1];
			newArray[i + 1] = newArray[i + 2];
			newArray[i + 2] = newArray[i + 3];
			newArray[i + 3] = alpha;
		}

		return newArray;
	}

	// Move this into the icon class
	private Image LoadImage(StatusNotifierItemProperties item)
	{
		if (!string.IsNullOrEmpty(item.IconThemePath))
		{
			var imageData = File.ReadAllBytes(System.IO.Path.Join(item.IconThemePath, item.IconName) +  ".png");
			var loader = PixbufLoader.NewWithType("png");
			loader.Write(imageData);

			return new Image(loader.Pixbuf.ScaleSimple(24, 24, InterpType.Bilinear));
		}

		if (!string.IsNullOrEmpty(item.IconName))
		{
			var iconTheme = IconTheme.GetForScreen(Screen);
			var pixbuf = iconTheme.LoadIcon(item.IconName, 24, IconLookupFlags.DirLtr);
			pixbuf = pixbuf.ScaleSimple(24, 24, InterpType.Bilinear);

			return new Image(pixbuf);
		}

		if (item.IconPixmap != null)
		{
			var biggestIcon = item.IconPixmap.MaxBy(i => i.Width * i.Height);
			var colorCorrectedIconData = ConvertArgbToRgba(biggestIcon.Data, biggestIcon.Width, biggestIcon.Height);
			var pixBuffer = new Pixbuf(colorCorrectedIconData, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, 4 * biggestIcon.Width);
			return new Image(pixBuffer.ScaleSimple(24, 24, InterpType.Bilinear));
		}

		Console.WriteLine("System Tray - Failed to find icon for: " + item.Title);
		return null;
	}
}
