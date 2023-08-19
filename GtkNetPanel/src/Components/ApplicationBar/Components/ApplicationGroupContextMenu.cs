using System.Reactive.Subjects;
using Gtk;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.Components.ApplicationBar.Components;

public class ApplicationGroupContextMenu : Menu
{
	private static readonly List<AllowedWindowActions> s_displayableActions = new()
	{
		AllowedWindowActions.Maximize,
		AllowedWindowActions.Minimize,
		AllowedWindowActions.Move,
		AllowedWindowActions.Resize
	};

	private readonly Subject<IconGroupViewModel> _buttonReleaseSubject = new();
	private readonly Subject<bool> _pinSubject = new();
	private readonly Subject<AllowedWindowActions> _windowAction = new();
	private readonly Subject<DesktopFileAction> _desktopFileAction = new();

	public ApplicationGroupContextMenu(IObservable<IconGroupViewModel> viewModel)
	{
		viewModel.Subscribe(vm =>
		{
			Children.ToList().ForEach(Remove);
			CreateContextMenu(vm);
		});
	}

	public IObservable<bool> Pin => _pinSubject;
	public IObservable<IconGroupViewModel> ButtonRelease => _buttonReleaseSubject;
	public IObservable<AllowedWindowActions> WindowAction => _windowAction;
	public Subject<DesktopFileAction> DesktopFileAction => _desktopFileAction;

	private void CreateContextMenu(IconGroupViewModel group)
	{
		var pinMenuItem = CreateMenuItem("Pin to Dock", "");
		pinMenuItem.ButtonReleaseEvent += (o, args) => _pinSubject.OnNext(true);
		Add(pinMenuItem);
		Add(new SeparatorMenuItem());

		var allowedActions = group.Tasks.First().AllowedActions;
		var desktopFile = group.Tasks.First().DesktopFile;

		foreach (var action in s_displayableActions)
		{
			var menuItem = CreateMenuItem(action.ToString(), "");
			menuItem.Sensitive = allowedActions.Contains(action);
			menuItem.ButtonReleaseEvent += (o, args) => _windowAction.OnNext(action);
			Add(menuItem);
		}

		if (desktopFile != null && desktopFile.Actions.Count > 0)
		{
			Add(new SeparatorMenuItem());

			foreach (var action in desktopFile.Actions)
			{
				var menuItem = CreateMenuItem(action.ActionName, action.IconName);
				menuItem.ButtonReleaseEvent += (o, args) => _desktopFileAction.OnNext(action);
				Add(menuItem);
			}
		}

		if (allowedActions.Contains(AllowedWindowActions.Close))
		{
			Add(new SeparatorMenuItem());
			var menuItem = CreateMenuItem("Close", "");
			menuItem.ButtonReleaseEvent += (o, args) => _windowAction.OnNext(AllowedWindowActions.Close);
			Add(menuItem);
		}

		ShowAll();
	}

	private MenuItem CreateMenuItem(string label, string iconName)
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
