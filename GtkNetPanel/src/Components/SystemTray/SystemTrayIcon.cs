using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Gtk;
using GtkNetPanel.Extensions.Gtk;
using GtkNetPanel.Services.DBus.Interfaces;
using GtkNetPanel.State;
using GtkNetPanel.State.SystemTray;
using Menu = Gtk.Menu;

namespace GtkNetPanel.Components.SystemTray;

public class SystemTrayIcon : Button
{
	private readonly Menu _contextMenu;
	private readonly Subject<int> _menuItemActivatedSubject = new();
	private readonly Subject<(int, int)> _applicationActivated = new();

	public SystemTrayIcon(IObservable<SystemTrayItemState> viewModelObservable)
	{
		_contextMenu = new Menu();

		this.CreateContextMenuObservable()
			.Where(_ => _contextMenu.Children.Any())
			.Subscribe(_ => _contextMenu.Popup());

		Valign = Align.Center;
		StyleContext.AddClass("system-tray__icon");
		var hasActivateMethod = false;

		viewModelObservable
			.TakeUntilDestroyed(this)
			.Select(s => s.Properties)
			.Take(1)
			.Subscribe(properties =>
			{
				TooltipText = properties.Title;
				HasTooltip = !string.IsNullOrEmpty(TooltipText);
				var image = new Image(properties.CreateIcon(IconTheme.GetForScreen(Screen)).ScaleSimple(24, 24, InterpType.Bilinear));
				image.Valign = Align.Center;
				Add(image);
				ShowAll();
			});

		viewModelObservable
			.TakeUntilDestroyed(this)
			.Select(s => s.StatusNotifierItemDescription)
			.DistinctUntilChanged()
			.Subscribe(desc =>
			{
				hasActivateMethod = desc.InterfaceHasMethod(OrgKdeStatusNotifierItem.Interface, "Activate");
			});

		viewModelObservable
			.TakeUntilDestroyed(this)
			.Select(s => s.RootMenuItem)
			.DistinctUntilChanged()
			.Subscribe(menuState =>
			{
				_contextMenu.RemoveAllChildren();
				DbusContextMenuHelpers.PopulateMenu(_contextMenu, menuState);
				var allMenuItems = DbusContextMenuHelpers.GetAllMenuItems(_contextMenu);
				foreach (var i in allMenuItems) i.Activated += (_, _) => _menuItemActivatedSubject.OnNext(i.GetDbusMenuItem().Id);
			});

		Observable.FromEventPattern<ButtonPressEventArgs>(this, nameof(ButtonPressEvent))
			.TakeUntilDestroyed(this)
			.Where(e => hasActivateMethod && e.EventArgs.Event.Button == 1 && e.EventArgs.Event.Type == EventType.DoubleButtonPress)
			.Select(e => e.EventArgs.Event)
			.Subscribe(e => _applicationActivated.OnNext(((int)e.XRoot, (int)e.YRoot)));

		Observable.FromEventPattern<ButtonPressEventArgs>(this, nameof(ButtonPressEvent))
			.TakeUntilDestroyed(this)
			.Where(e => !hasActivateMethod && e.EventArgs.Event.Button == 1 && e.EventArgs.Event.Type == EventType.ButtonPress)
			.Subscribe(_ => _contextMenu.Popup());
	}

	public IObservable<int> MenuItemActivated => _menuItemActivatedSubject;
	public IObservable<(int, int)> ApplicationActivated => _applicationActivated;

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		_menuItemActivatedSubject.OnCompleted();
		_applicationActivated.OnCompleted();
		_contextMenu.Destroy();
	}
}
