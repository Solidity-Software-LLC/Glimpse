using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.DBus.Interfaces;
using Glimpse.State.SystemTray;
using Gtk;
using Menu = Gtk.Menu;

namespace Glimpse.Components.SystemTray;

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

		var image = new Image();
		image.Valign = Align.Center;
		Add(image);

		var propertiesObservable = viewModelObservable
			.TakeUntilDestroyed(this)
			.Select(s => s.Properties)
			.DistinctUntilChanged();

		propertiesObservable
			.DistinctUntilChanged((x, y) => x.Title == y.Title)
			.Subscribe(properties =>
			{
				TooltipText = properties.Title;
				HasTooltip = !string.IsNullOrEmpty(TooltipText);
			});

		propertiesObservable
			.DistinctUntilChanged((x, y) => x.IconName == y.IconName && x.IconThemePath == y.IconThemePath && x.IconPixmap == y.IconPixmap)
			.Subscribe(properties =>
			{
				image.Pixbuf = properties.CreateIcon(IconTheme.GetForScreen(Screen)).ScaleSimple(24, 24, InterpType.Bilinear);
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

		this.ObserveButtonRelease()
			.Where(e => hasActivateMethod && e.Event.Button == 1)
			.Subscribe(e => _applicationActivated.OnNext(((int)e.Event.XRoot, (int)e.Event.YRoot)));

		this.ObserveButtonRelease()
			.Where(e => !hasActivateMethod && e.Event.Button == 1)
			.Subscribe(_ => _contextMenu.Popup());

		ShowAll();
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
