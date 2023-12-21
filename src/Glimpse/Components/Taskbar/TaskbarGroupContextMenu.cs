using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Glimpse.Components.Shared;
using Glimpse.Extensions;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Gtk;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.Components.Taskbar;

public class TaskbarGroupContextMenu : Menu
{
	private readonly Subject<bool> _pinSubject = new();
	private readonly Subject<AllowedWindowActions> _windowAction = new();
	private readonly Subject<DesktopFileAction> _desktopFileAction = new();
	private readonly Subject<DesktopFile> _launch = new();
	private static readonly Pixbuf s_closeIcon = Assets.Close.Scale(ThemeConstants.MenuItemIconSize);
	private static readonly Pixbuf s_unpinIcon = Assets.UnpinIcon.Scale(ThemeConstants.MenuItemIconSize);
	private static readonly Pixbuf s_pinIcon = Assets.PinIcon.Scale(ThemeConstants.MenuItemIconSize);

	public TaskbarGroupContextMenu(IObservable<TaskbarGroupContextMenuViewModel> viewModel)
	{
		ReserveToggleSize = false;

		viewModel.Subscribe(vm =>
		{
			Children.ToList().ForEach(c => c.Destroy());
			CreateContextMenu(vm);
		});

		this.Events().Destroyed.Take(1).Subscribe(_ =>
		{
			_pinSubject.OnCompleted();
			_windowAction.OnCompleted();
			_desktopFileAction.OnCompleted();
			_launch.OnCompleted();
		});
	}

	public IObservable<bool> Pin => _pinSubject;
	public IObservable<AllowedWindowActions> WindowAction => _windowAction;
	public IObservable<DesktopFileAction> DesktopFileAction => _desktopFileAction;
	public IObservable<DesktopFile> Launch => _launch;

	private void CreateContextMenu(TaskbarGroupContextMenuViewModel viewModel)
	{
		CreateDesktopFileActions(viewModel.DesktopFile, viewModel.ActionIcons).ForEach(Add);

		if (viewModel.DesktopFile.IniFile != null)
		{
			var launchIcon = viewModel.LaunchIcon.Scale(ThemeConstants.MenuItemIconSize);
			var launchMenuItem = CreateLaunchMenuItem(viewModel, launchIcon);
			launchMenuItem.Events().Destroyed.Take(1).Subscribe(_ => launchIcon.Dispose());
			Add(launchMenuItem);
			Add(CreatePinMenuItem(viewModel));
		}

		if (viewModel.CanClose && CreateCloseAction(viewModel) is { } closeAction)
		{
			Add(closeAction);
		}

		ShowAll();
	}

	private MenuItem CreatePinMenuItem(TaskbarGroupContextMenuViewModel viewModel)
	{
		var pinLabel = viewModel.IsPinned ? "Unpin from taskbar" : "Pin to taskbar";
		var icon = viewModel.IsPinned ? s_unpinIcon : s_pinIcon;
		var pinMenuItem = ContextMenuHelper.CreateMenuItem(pinLabel, icon);
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
			var menuItem = ContextMenuHelper.CreateMenuItem("Close Window", s_closeIcon);
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
