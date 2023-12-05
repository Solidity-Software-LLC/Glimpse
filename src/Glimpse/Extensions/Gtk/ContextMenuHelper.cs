using System.Reactive.Linq;
using Gdk;
using Glimpse.Components.Shared;
using Glimpse.Services.FreeDesktop;
using Gtk;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.Extensions.Gtk;

public static class ContextMenuHelper
{
	public static List<MenuItem> CreateDesktopFileActions(DesktopFile desktopFile, Dictionary<string, Pixbuf> icons)
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
				var actionIcon = icons[action.ActionName].Scale(ThemeConstants.MenuItemIconSize);
				var menuItem = CreateMenuItem(action.ActionName, actionIcon);
				menuItem.Events().Destroyed.Take(1).Subscribe(_ => actionIcon.Dispose());
				menuItem.Data.Add("DesktopFileAction", action);
				results.Add(menuItem);
			}
		}

		return results;
	}

	public static MenuItem CreateMenuItem(string label, Pixbuf icon)
	{
		var image = new Image();
		image.Pixbuf = icon;

		var box = new Box(Orientation.Horizontal, 6);
		box.Add(image);
		box.Add(new Label(label));

		var menuItem = new MenuItem();
		menuItem.Add(box);

		return menuItem;
	}
}
