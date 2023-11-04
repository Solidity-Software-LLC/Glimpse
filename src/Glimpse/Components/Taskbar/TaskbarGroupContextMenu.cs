using System.Reactive.Subjects;
using Gdk;
using Glimpse.Components.Shared;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Gtk;

namespace Glimpse.Components.Taskbar;

public class TaskbarGroupContextMenu : Menu
{
	private readonly Subject<bool> _pinSubject = new();
	private readonly Subject<AllowedWindowActions> _windowAction = new();
	private readonly Subject<DesktopFileAction> _desktopFileAction = new();
	private readonly Subject<DesktopFile> _launch = new();

	public TaskbarGroupContextMenu(IObservable<TaskbarGroupContextMenuViewModel> viewModel)
	{
		ReserveToggleSize = false;

		viewModel.Subscribe(vm =>
		{
			Children.ToList().ForEach(Remove);
			CreateContextMenu(vm);
		});
	}

	public IObservable<bool> Pin => _pinSubject;
	public IObservable<AllowedWindowActions> WindowAction => _windowAction;
	public Subject<DesktopFileAction> DesktopFileAction => _desktopFileAction;
	public Subject<DesktopFile> Launch => _launch;

	private void CreateContextMenu(TaskbarGroupContextMenuViewModel viewModel)
	{
		var launchIcon = viewModel.LaunchIcon.Scale(ThemeConstants.MenuItemIconSize);
		CreateDesktopFileActions(viewModel.DesktopFile, viewModel.ActionIcons).ForEach(Add);
		Add(CreateLaunchMenuItem(viewModel, launchIcon));
		Add(CreatePinMenuItem(viewModel));
		if (viewModel.CanClose && CreateCloseAction(viewModel) is { } closeAction) Add(closeAction);
		ShowAll();
	}

	private MenuItem CreatePinMenuItem(TaskbarGroupContextMenuViewModel viewModel)
	{
		var pinLabel = viewModel.IsPinned ? "Unpin from taskbar" : "Pin to taskbar";
		var icon = viewModel.IsPinned ? Assets.UnpinIcon : Assets.PinIcon;
		var pinMenuItem = ContextMenuHelper.CreateMenuItem(pinLabel, icon.Scale(ThemeConstants.MenuItemIconSize));
		pinMenuItem.ObserveButtonRelease().Subscribe(_ => _pinSubject.OnNext(true));
		return pinMenuItem;
	}

	private MenuItem CreateLaunchMenuItem(TaskbarGroupContextMenuViewModel viewModel, Pixbuf icon)
	{
		var pinMenuItem = ContextMenuHelper.CreateMenuItem(viewModel.DesktopFile.Name, icon);
		pinMenuItem.ObserveButtonRelease().Subscribe(_ => _launch.OnNext(viewModel.DesktopFile));
		return pinMenuItem;
	}

	private MenuItem CreateCloseAction(TaskbarGroupContextMenuViewModel viewModel)
	{
		if (viewModel.CanClose)
		{
			var menuItem = ContextMenuHelper.CreateMenuItem("Close Window", Assets.Close.Scale(ThemeConstants.MenuItemIconSize));
			menuItem.ObserveButtonRelease().Subscribe(_ => _windowAction.OnNext(AllowedWindowActions.Close));
			return menuItem;
		}

		return null;
	}

	private List<MenuItem> CreateDesktopFileActions(DesktopFile desktopFile, Dictionary<string, Pixbuf> actionIcons)
	{
		var menuItems = ContextMenuHelper.CreateDesktopFileActions(desktopFile, actionIcons);

		if (menuItems.Any())
		{
			menuItems.Add(new SeparatorMenuItem());

			menuItems.ForEach(m =>
			{
				var action = (DesktopFileAction)m.Data["DesktopFileAction"];
				m.ObserveButtonRelease().Subscribe(_ => _desktopFileAction.OnNext(action));
			});
		}

		return menuItems;
	}
}
