using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Gtk;
using GtkNetPanel.Components.ContextMenu;
using GtkNetPanel.Services.DBus.StatusNotifierItem;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.Services.SystemTray;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.SystemTray;

public class SystemTrayIcon : EventBox
{
	private readonly Menu _contextMenu;
	private readonly LinkedList<IDisposable> _disposables = new();
	private readonly ContextMenuHelper _helper;
	private readonly Subject<int> _menuItemActivatedSubject = new();
	private readonly Subject<(int, int)> _applicationActivated = new();
	private Image _icon;

	public SystemTrayIcon(IObservable<SystemTrayItemState> trayItemStateObservable)
	{
		_contextMenu = new Menu();
		_helper = new ContextMenuHelper(this);
		_disposables.AddLast(_helper);

		AddEvents((int)EventMask.ButtonReleaseMask);
		var hasActivateMethod = false;

		_disposables.AddLast(
			trayItemStateObservable.Select(s => s.Properties).DistinctUntilChanged().Subscribe(properties =>
			{
				CreateTrayIcon(properties);
				TooltipText = properties.Category;
				HasTooltip = !string.IsNullOrEmpty(TooltipText);
			}));

		_disposables.AddLast(
			trayItemStateObservable.Select(s => s.StatusNotifierItemDescription).DistinctUntilChanged().Subscribe(desc =>
			{
				hasActivateMethod = desc.InterfaceHasMethod(IStatusNotifierItem.DbusInterfaceName, "Activate");
			}));

		_disposables.AddLast(
			trayItemStateObservable.Select(s => s.RootSystemTrayMenuItem).DistinctUntilChanged().Subscribe(menuState =>
			{
				_contextMenu.RemoveAllChildren();
				DbusContextMenuHelpers.PopulateMenu(_contextMenu, menuState);
				var allMenuItems = DbusContextMenuHelpers.GetAllMenuItems(_contextMenu);
				foreach (var i in allMenuItems) i.Activated += (_, _) => _menuItemActivatedSubject.OnNext(i.GetDbusMenuItem().Id);
			}));

		_disposables.AddLast(
			Observable.FromEventPattern<EventArgs>(_helper, nameof(_helper.ContextMenu))
				.Where(_ => _contextMenu.Children.Any())
				.Subscribe(_ => _contextMenu.Popup()));

		_disposables.AddLast(
			Observable.FromEventPattern<ButtonPressEventArgs>(this, nameof(ButtonPressEvent))
				.Where(e => hasActivateMethod && e.EventArgs.Event.Button == 1 && e.EventArgs.Event.Type == EventType.DoubleButtonPress)
				.Select(e => e.EventArgs.Event)
				.Subscribe(e => _applicationActivated.OnNext(((int)e.XRoot, (int)e.YRoot))));

		_disposables.AddLast(
			Observable.FromEventPattern<ButtonPressEventArgs>(this, nameof(ButtonPressEvent))
				.Where(e => !hasActivateMethod && e.EventArgs.Event.Button == 1 && e.EventArgs.Event.Type == EventType.ButtonPress)
				.Subscribe(_ => _contextMenu.Popup()));
	}

	public IObservable<int> MenuItemActivated => _menuItemActivatedSubject;
	public IObservable<(int, int)> ApplicationActivated => _applicationActivated;

	private void CreateTrayIcon(StatusNotifierItemProperties properties)
	{
		if (_icon != null)
		{
			Remove(_icon);
		}

		_icon = new Image(properties
			.CreateIcon(IconTheme.GetForScreen(Screen))
			.ScaleSimple(24, 24, InterpType.Bilinear));

		Add(_icon);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		foreach (var d in _disposables)
		{
			d.Dispose();
		}

		_contextMenu.Destroy();
	}
}
