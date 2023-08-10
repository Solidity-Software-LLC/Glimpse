using System.Reactive.Linq;
using Gdk;
using Gtk;
using GtkNetPanel.Components.ContextMenu;
using GtkNetPanel.Services.DBus;
using GtkNetPanel.Services.DBus.StatusNotifierItem;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.Tray;

public class SystemTrayIcon : EventBox
{
	private readonly Menu _contextMenu;
	private Image _icon;
	private readonly LinkedList<IDisposable> _disposables = new();
	private readonly ContextMenuHelper _helper;

	public SystemTrayIcon(IObservable<TrayItemState> trayItemStateObservable)
	{
		_contextMenu = new Menu();
		_helper = new ContextMenuHelper(this);
		_disposables.AddLast(_helper);

		AddEvents((int)EventMask.ButtonReleaseMask);
		DbusStatusNotifierItem currentStatus = null;
		var hasActivateMethod = false;

		_disposables.AddLast(
			trayItemStateObservable.Select(s => s.Status).DistinctUntilChanged().Subscribe(statusState =>
			{
				currentStatus = statusState;
				hasActivateMethod = currentStatus.Object.InterfaceHasMethod(IStatusNotifierItem.DbusInterfaceName, "Activate");

				CreateTrayIcon(statusState);
				TooltipText = statusState.Properties.Category;
				HasTooltip = !string.IsNullOrEmpty(TooltipText);
			}));

		_disposables.AddLast(
			trayItemStateObservable.Select(s => s.RootMenuItem).DistinctUntilChanged().Subscribe(menuState =>
			{
				_contextMenu.RemoveAllChildren();
				DbusContextMenuHelpers.PopulateMenu(_contextMenu, menuState);
				var allMenuItems = DbusContextMenuHelpers.GetAllMenuItems(_contextMenu);

				foreach (var i in allMenuItems)
				{
					i.Activated += (sender, args) => DBus.ClickedItem(currentStatus, i.GetDbusMenuItem().Id);
				}
			}));

		_disposables.AddLast(
			Observable.FromEventPattern<EventArgs>(_helper, nameof(_helper.ContextMenu))
				.Where(_ => _contextMenu.Children.Any())
				.Subscribe(_ => _contextMenu.Popup()));

		_disposables.AddLast(
			Observable.FromEventPattern<ButtonPressEventArgs>(this, nameof(ButtonPressEvent))
				.Where(e => hasActivateMethod && e.EventArgs.Event.Button == 1 && e.EventArgs.Event.Type == EventType.DoubleButtonPress)
				.Select(e => e.EventArgs.Event)
				.Subscribe(e => DBus.ActivateSystemTrayItemAsync(currentStatus, (int)e.XRoot, (int)e.YRoot)));

		_disposables.AddLast(
			Observable.FromEventPattern<ButtonPressEventArgs>(this, nameof(ButtonPressEvent))
				.Where(e => !hasActivateMethod && e.EventArgs.Event.Button == 1 && e.EventArgs.Event.Type == EventType.ButtonPress)
				.Subscribe(_ => _contextMenu.Popup()));
	}

	private void CreateTrayIcon(DbusStatusNotifierItem state)
	{
		if (_icon != null)
		{
			Remove(_icon);
		}

		_icon = new Image(state
			.CreateIcon(IconTheme.GetForScreen(Screen))
			.ScaleSimple(24, 24, InterpType.Bilinear));

		Add(_icon);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		foreach (var d in _disposables) d.Dispose();
		_contextMenu.Destroy();
	}
}
