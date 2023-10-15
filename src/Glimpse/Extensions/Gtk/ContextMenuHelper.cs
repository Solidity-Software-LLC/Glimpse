using System.Reactive.Linq;
using Gdk;
using Glimpse.Components.Shared;
using Glimpse.Services.FreeDesktop;
using Gtk;

namespace Glimpse.Extensions.Gtk;

public static class ContextMenuHelper
{
	public static List<MenuItem> CreateDesktopFileActions(DesktopFile desktopFile, IconTheme iconTheme)
	{
		var results = new List<MenuItem>();

		if (desktopFile != null && desktopFile.Actions.Count > 0)
		{
			var headerLabel = new Label("Tasks");
			headerLabel.Halign = Align.Start;
			headerLabel.StyleContext.AddClass("header-menu-item-label");

			var header = new SeparatorMenuItem();
			header.StyleContext.AddClass("header-menu-item");
			header.Add(headerLabel);

			results.Add(header);

			foreach (var action in desktopFile.Actions)
			{
				var iconObservable = Observable.Return(iconTheme.LoadIcon(action.IconName, ThemeConstants.MenuItemIconSize)).Concat(iconTheme.ObserveChange().Select(t => t.LoadIcon(action.IconName, ThemeConstants.MenuItemIconSize)));
				var menuItem = CreateMenuItem(action.ActionName, iconObservable);
				menuItem.Data.Add("DesktopFileAction", action);
				results.Add(menuItem);
			}
		}

		return results;
	}

	public static MenuItem CreateMenuItem(string label, IObservable<Pixbuf> iconObservable)
	{
		var image = new Image();

		var box = new Box(Orientation.Horizontal, 6);
		box.Add(image);
		box.Add(new Label(label));

		var menuItem = new MenuItem();
		menuItem.Add(box);

		iconObservable.TakeUntilDestroyed(menuItem).Subscribe(icon => image.Pixbuf = icon);

		return menuItem;
	}
}
