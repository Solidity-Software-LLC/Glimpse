using Gdk;
using ReactiveMarbles.ObservableEvents;
using Unit = System.Reactive.Unit;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.UI.Components.Notifications;

public class NotificationWindow : Window
{
	private readonly NotificationContent _content;

	public NotificationWindow(IObservable<NotificationViewModel> notificationStateObs) : base(WindowType.Toplevel)
	{
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = false;
		TypeHint = WindowTypeHint.Notification;
		Visual = Screen.RgbaVisual;
		AppPaintable = true;
		Visible = false;
		KeepAbove = true;

		this.Events().DeleteEvent.Subscribe(e => e.RetVal = true);

		_content = new NotificationContent(notificationStateObs);
		Add(_content);
		ShowAll();
	}

	public IObservable<string> ActionInvoked => _content.ActionInvoked;
	public IObservable<Unit> CloseNotification => _content.CloseNotification;
}
