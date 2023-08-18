using Gtk;
using GtkNetPanel.Services.DisplayServer;
using Action = System.Action;

namespace GtkNetPanel.Components.ApplicationBar.Components;

public class ApplicationGroupContextMenu : Menu
{
	private readonly List<AllowedWindowActions> _displayableActions = new()
	{
		AllowedWindowActions.Maximize,
		AllowedWindowActions.Minimize,
		AllowedWindowActions.Move,
		AllowedWindowActions.Resize
	};

	public ApplicationGroupContextMenu(IObservable<IconGroupViewModel> viewModel)
	{
		viewModel.Subscribe(vm =>
		{
			Children.ToList().ForEach(Remove);
			CreateContextMenu(vm);
		});
	}

	private void CreateContextMenu(IconGroupViewModel group)
	{
		Add(CreateMenuItem("Pin to Dock", "", () => { }));
		Add(new SeparatorMenuItem());

		var allowedActions = group.Tasks.First().AllowedActions;
		var desktopFile = group.Tasks.First().DesktopFile;

		foreach (var action in _displayableActions)
		{
			var menuItem = CreateMenuItem(action.ToString(), "", () => { });
			menuItem.Sensitive = allowedActions.Contains(action);
			Add(menuItem);
		}

		if (desktopFile != null && desktopFile.Actions.Count > 0)
		{
			Add(new SeparatorMenuItem());

			foreach (var action in desktopFile.Actions)
			{
				Add(CreateMenuItem(action.ActionName, action.IconName, () => { }));
			}
		}

		if (allowedActions.Contains(AllowedWindowActions.Close))
		{
			Add(new SeparatorMenuItem());
			Add(CreateMenuItem("Close", "", () => { }));
		}

		ShowAll();
	}

	private MenuItem CreateMenuItem(string label, string iconName, Action action)
	{
		var box = new Box(Orientation.Horizontal, 0);

		if (!string.IsNullOrEmpty(iconName))
		{
			box.PackStart(Image.NewFromIconName(iconName, IconSize.Menu), false, false, 0);
		}

		box.PackStart(new Label(label), false, false, 0);
		var menuItem = new MenuItem();
		menuItem.Add(box);
		return menuItem;
	}
}
