using System.Reactive.Subjects;
using Gdk;
using Glimpse.Common.Gtk;
using Gtk;
using Pango;
using ReactiveMarbles.ObservableEvents;
using Unit = System.Reactive.Unit;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;
using WrapMode = Pango.WrapMode;

namespace Glimpse.UI.Components.Notifications;

public class NotificationContent : Bin
{
	private readonly Subject<Unit> _closeSubject = new();
	public IObservable<Unit> CloseNotification => _closeSubject;

	public NotificationContent(IObservable<NotificationViewModel> notificationStateObs)
	{
		var appName = new Label();
		appName.AddClass("notifications__app-name");
		appName.Halign = Align.Fill;
		appName.Ellipsize = EllipsizeMode.End;
		appName.MaxWidthChars = 1;
		appName.Hexpand = true;
		appName.Xalign = 0;

		var summary = new Label();
		summary.AddClass("notifications__summary");
		summary.Halign = Align.Fill;
		summary.Ellipsize = EllipsizeMode.End;
		summary.MaxWidthChars = 1;
		summary.Hexpand = true;
		summary.Xalign = 0;
		summary.Yalign = 0;

		var body = new Label();
		body.AddClass("notifications__body");
		body.Ellipsize = EllipsizeMode.End;
		body.Lines = 5;
		body.LineWrap = true;
		body.LineWrapMode = WrapMode.Word;
		body.MaxWidthChars = 1;
		body.Xalign = 0;
		body.Yalign = 0;

		var closeButton = new Button();
		closeButton.AddButtonStates();
		closeButton.Image = new Image(Assets.Close.Scale(16).ToPixbuf());
		closeButton.Halign = Align.End;
		closeButton.ObserveButtonRelease().Subscribe(_ => _closeSubject.OnNext(Unit.Default));

		var appIcon = new Image();
		var image = new Image().AddClass("notifications__image");
		image.Xalign = 0;

		var appNameRow = new Box(Orientation.Horizontal, 4);
		appNameRow.AddMany(appIcon, appName, closeButton);

		var textColumn = new Box(Orientation.Vertical, 0);
		textColumn.AddMany(summary, body);

		var mainRow = new Box(Orientation.Horizontal, 0);
		mainRow.AddMany(image, textColumn);

		var actionsRow = new Box(Orientation.Horizontal, 8);
		actionsRow.Halign = Align.End;

		var layout = new Box(Orientation.Vertical, 0)
			.AddClass("notifications__window")
			.AddMany(appNameRow, mainRow, actionsRow);

		Add(layout);

		notificationStateObs.Subscribe(n =>
		{
			image.Pixbuf = n.Image?.ScaleToFit(48, 100).ToPixbuf();
			image.Visible = image.Pixbuf != null;
			appIcon.Pixbuf = n.AppIcon?.Scale(20).ToPixbuf();
			appIcon.Visible = appIcon.Pixbuf != null;
			appName.Text = n.AppName;
			summary.Text = n.Summary;
			body.Text = n.Body;
			body.Visible = !string.IsNullOrEmpty(n.Body);

			actionsRow.RemoveAllChildren();

			foreach (var action in n.Actions)
			{
				var actionButton = new Button() { Halign = Align.End, Label = action };
				actionButton.AddButtonStates();
				actionButton.AddClass("notifications_action-button");
				actionButton.ShowAll();
				actionsRow.Add(actionButton);
			}
		});

		ShowAll();
	}
}

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

	public IObservable<Unit> CloseNotification => _content.CloseNotification;
}
