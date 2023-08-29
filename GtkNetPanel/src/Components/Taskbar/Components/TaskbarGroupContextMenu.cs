using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.Components.Taskbar.Components;

public class TaskbarGroupContextMenu : Menu
{
	private readonly Subject<bool> _pinSubject = new();
	private readonly Subject<AllowedWindowActions> _windowAction = new();
	private readonly Subject<DesktopFileAction> _desktopFileAction = new();
	private readonly Subject<DesktopFile> _launch = new();

	public TaskbarGroupContextMenu(IObservable<ApplicationBarGroupViewModel> viewModel)
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

	private void CreateContextMenu(ApplicationBarGroupViewModel barGroup)
	{
		if (barGroup.Tasks.Count == 0)
		{
			CreateDesktopFileActions(barGroup.DesktopFile).ForEach(Add);
			Add(CreateLaunchMenuItem(barGroup));
			Add(CreatePinMenuItem(barGroup));
		}
		else
		{
			CreateDesktopFileActions(barGroup.DesktopFile).ForEach(Add);
			Add(CreateLaunchMenuItem(barGroup));
			Add(CreatePinMenuItem(barGroup));
			if (CreateCloseAction(barGroup) is { } closeAction) Add(closeAction);
		}

		ShowAll();
	}

	private MenuItem CreatePinMenuItem(ApplicationBarGroupViewModel group)
	{
		var pinLabel = group.IsPinned ? "Unpin from taskbar" : "Pin to taskbar";
		var icon = group.IsPinned ? Assets.UnpinIcon : Assets.PinIcon;
		var pinMenuItem = ContextMenuHelper.CreateMenuItem(pinLabel, icon.ScaleSimple(16, 16, InterpType.Bilinear));
		pinMenuItem.ButtonReleaseEvent += (o, args) => _pinSubject.OnNext(true);
		return pinMenuItem;
	}

	private MenuItem CreateLaunchMenuItem(ApplicationBarGroupViewModel group)
	{
		var pinMenuItem = ContextMenuHelper.CreateMenuItem(group.DesktopFile.Name, IconLoader.LoadIcon(group.DesktopFile.IconName, 16));
		pinMenuItem.ButtonReleaseEvent += (o, args) => _launch.OnNext(group.DesktopFile);
		return pinMenuItem;
	}

	private MenuItem CreateCloseAction(ApplicationBarGroupViewModel barGroup)
	{
		var allowedActions = barGroup.Tasks.First().AllowedActions;

		if (allowedActions.Contains(AllowedWindowActions.Close))
		{
			var menuItem = ContextMenuHelper.CreateMenuItem("Close", Assets.Close.ScaleSimple(16, 16, InterpType.Bilinear));
			menuItem.ButtonReleaseEvent += (o, args) => _windowAction.OnNext(AllowedWindowActions.Close);
			return menuItem;
		}

		return null;
	}

	private List<MenuItem> CreateDesktopFileActions(DesktopFile desktopFile)
	{
		var menuItems = ContextMenuHelper.CreateDesktopFileActions(desktopFile);
		menuItems.Add(new SeparatorMenuItem());

		menuItems.ForEach(m =>
		{
			var action = (DesktopFileAction)m.Data["DesktopFileAction"];

			Observable.FromEventPattern(m, nameof(m.ButtonReleaseEvent))
				.TakeUntilDestroyed(m)
				.Subscribe(_ => _desktopFileAction.OnNext(action));
		});

		return menuItems;
	}
}
