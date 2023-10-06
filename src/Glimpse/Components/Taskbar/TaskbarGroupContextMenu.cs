using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
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

	public TaskbarGroupContextMenu(IObservable<TaskbarGroupViewModel> viewModel)
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

	private void CreateContextMenu(TaskbarGroupViewModel barGroup)
	{
		var iconTheme = IconTheme.GetForScreen(Screen);
		var iconObservable = Observable.Return(iconTheme.LoadIcon(barGroup, 16)).Concat(iconTheme.ObserveChange().Select(_ => iconTheme.LoadIcon(barGroup, 16)));

		if (barGroup.Tasks.Count == 0)
		{
			CreateDesktopFileActions(barGroup.DesktopFile).ForEach(Add);
			Add(CreateLaunchMenuItem(barGroup, iconObservable));
			Add(CreatePinMenuItem(barGroup));
		}
		else
		{
			CreateDesktopFileActions(barGroup.DesktopFile).ForEach(Add);
			Add(CreateLaunchMenuItem(barGroup, iconObservable));
			Add(CreatePinMenuItem(barGroup));
			if (CreateCloseAction(barGroup) is { } closeAction) Add(closeAction);
		}

		ShowAll();
	}

	private MenuItem CreatePinMenuItem(TaskbarGroupViewModel group)
	{
		var pinLabel = group.IsPinned ? "Unpin from taskbar" : "Pin to taskbar";
		var icon = group.IsPinned ? Assets.UnpinIcon : Assets.PinIcon;
		var pinMenuItem = ContextMenuHelper.CreateMenuItem(pinLabel, Observable.Return(icon.ScaleSimple(16, 16, InterpType.Bilinear)));
		pinMenuItem.ObserveButtonRelease().Subscribe(_ => _pinSubject.OnNext(true));
		return pinMenuItem;
	}

	private MenuItem CreateLaunchMenuItem(TaskbarGroupViewModel group, IObservable<Pixbuf> iconObservable)
	{
		var pinMenuItem = ContextMenuHelper.CreateMenuItem(group.DesktopFile.Name, iconObservable);
		pinMenuItem.ObserveButtonRelease().Subscribe(_ => _launch.OnNext(group.DesktopFile));
		return pinMenuItem;
	}

	private MenuItem CreateCloseAction(TaskbarGroupViewModel barGroup)
	{
		var allowedActions = barGroup.Tasks.First().AllowedActions;

		if (allowedActions.Contains(AllowedWindowActions.Close))
		{
			var menuItem = ContextMenuHelper.CreateMenuItem("Close Window", Observable.Return(Assets.Close.ScaleSimple(16, 16, InterpType.Bilinear)));
			menuItem.ObserveButtonRelease().Subscribe(_ => _windowAction.OnNext(AllowedWindowActions.Close));
			return menuItem;
		}

		return null;
	}

	private List<MenuItem> CreateDesktopFileActions(DesktopFile desktopFile)
	{
		var iconTheme = IconTheme.GetForScreen(Screen);
		var menuItems = ContextMenuHelper.CreateDesktopFileActions(desktopFile, iconTheme);

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
