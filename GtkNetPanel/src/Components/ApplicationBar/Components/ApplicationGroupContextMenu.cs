using System.Reactive.Subjects;
using Gdk;
using Gtk;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;

namespace GtkNetPanel.Components.ApplicationBar.Components;

public class ApplicationGroupContextMenu : Menu
{
	private static readonly Pixbuf s_unpinIcon;
	private static readonly Pixbuf s_pinIcon;
	private static readonly Pixbuf s_close;

	static ApplicationGroupContextMenu()
	{
		s_unpinIcon = Pixbuf.LoadFromResource("GtkNetPanel.unpin.png");
		s_pinIcon = Pixbuf.LoadFromResource("GtkNetPanel.pin.png");
		s_close = Pixbuf.LoadFromResource("GtkNetPanel.close.png");
	}

	private readonly Subject<bool> _pinSubject = new();
	private readonly Subject<AllowedWindowActions> _windowAction = new();
	private readonly Subject<DesktopFileAction> _desktopFileAction = new();
	private readonly Subject<DesktopFile> _launch = new();

	public ApplicationGroupContextMenu(IObservable<ApplicationBarGroupViewModel> viewModel)
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
			CreateDesktopFileActions(barGroup).ForEach(Add);
			Add(CreateLaunchMenuItem(barGroup));
			Add(CreatePinMenuItem(barGroup));
		}
		else
		{
			CreateDesktopFileActions(barGroup).ForEach(Add);
			Add(CreateLaunchMenuItem(barGroup));
			Add(CreatePinMenuItem(barGroup));
			var closeAction = CreateCloseAction(barGroup);
			if (closeAction != null) Add(closeAction);
		}

		ShowAll();
	}

	private MenuItem CreatePinMenuItem(ApplicationBarGroupViewModel group)
	{
		var pinLabel = group.IsPinned ? "Unpin from taskbar" : "Pin to taskbar";
		var icon = group.IsPinned ? s_unpinIcon : s_pinIcon;
		var pinMenuItem = CreateMenuItem(pinLabel, icon);
		pinMenuItem.ButtonReleaseEvent += (o, args) => _pinSubject.OnNext(true);
		return pinMenuItem;
	}

	private MenuItem CreateLaunchMenuItem(ApplicationBarGroupViewModel group)
	{
		var pinMenuItem = CreateMenuItem(group.DesktopFile.Name, group.DesktopFile.IconName);
		pinMenuItem.ButtonReleaseEvent += (o, args) => _launch.OnNext(group.DesktopFile);
		return pinMenuItem;
	}

	private MenuItem CreateCloseAction(ApplicationBarGroupViewModel barGroup)
	{
		var allowedActions = barGroup.Tasks.First().AllowedActions;

		if (allowedActions.Contains(AllowedWindowActions.Close))
		{
			var menuItem = CreateMenuItem("Close", s_close);
			menuItem.ButtonReleaseEvent += (o, args) => _windowAction.OnNext(AllowedWindowActions.Close);
			return menuItem;
		}

		return null;
	}

	private List<MenuItem> CreateDesktopFileActions(ApplicationBarGroupViewModel barGroup)
	{
		var results = new List<MenuItem>();
		var desktopFile = barGroup.DesktopFile;

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
				var menuItem = CreateMenuItem(action.ActionName, action.IconName);
				menuItem.ButtonReleaseEvent += (o, args) => _desktopFileAction.OnNext(action);
				results.Add(menuItem);
			}

			results.Add(new SeparatorMenuItem());
		}

		return results;
	}

	private MenuItem CreateMenuItem(string label, string iconName)
	{
		Pixbuf imageBuffer = null;

		if (!string.IsNullOrEmpty(iconName))
		{
			if (iconName.StartsWith("/"))
			{
				imageBuffer = new Pixbuf(File.ReadAllBytes(iconName));
			}
			else
			{
				imageBuffer = IconTheme.GetForScreen(Screen).LoadIcon(iconName, 26, IconLookupFlags.DirLtr);
			}
		}

		return CreateMenuItem(label, imageBuffer);
	}

	private MenuItem CreateMenuItem(string label, Pixbuf icon)
	{
		var image = new Image();
		image.Pixbuf = icon?.ScaleSimple(16, 16, InterpType.Bilinear);

		var box = new Box(Orientation.Horizontal, 6);
		box.Add(image);
		box.Add(new Label(label));

		var menuItem = new MenuItem();
		menuItem.Add(box);
		return menuItem;
	}
}
