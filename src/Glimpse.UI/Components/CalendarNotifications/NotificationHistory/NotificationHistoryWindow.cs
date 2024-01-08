using System.Reactive.Linq;
using GLib;
using Glimpse.Common.System.Reactive;
using Glimpse.Freedesktop.Notifications;
using Glimpse.Redux;
using Glimpse.UI.Components.Shared.Accordion;
using Gtk;
using Pango;
using WrapMode = Pango.WrapMode;

namespace Glimpse.UI.Components.CalendarNotifications.NotificationHistory;

public class NotificationHistoryWindow : Bin
{
	private readonly NotificationsService _notificationsService;

	public NotificationHistoryWindow(ReduxStore store, NotificationsService notificationsService)
	{
		_notificationsService = notificationsService;

		var accordion = new Accordion();
		accordion.Expand = true;

		var viewModelObs = store
			.Select(NotificationCalendarSelectors.ViewModel)
			.Select(vm => vm.NotificationHistory.OrderBy(n => n.AppName).ThenBy(n => n.CreationDate))
			.ObserveOn(new GLibSynchronizationContext())
			.Replay(1);

		viewModelObs
			.UnbundleMany(e => e.AppName)
			.RemoveIndex()
			.Subscribe(obs =>
			{
				accordion.AddSection(obs.Key.AppName,
					new Box(Orientation.Horizontal, 4)
						.AddClass("notification-history__section-header")
						.Prop(b => b.Halign = Align.Fill)
						.AddMany(new Image().BindViewModel(obs.Select(x => x.AppIcon), 16))
						.AddMany(new Label(obs.Key.AppName)));
			});

		viewModelObs
			.UnbundleMany(e => e.Id)
			.RemoveIndex()
			.Subscribe(o =>
			{
				var item = CreateNotificationEntry(o);
				accordion.AddItemToSection(o.Key.AppName, item);
				o.TakeLast(1).Subscribe(_ => item.Destroy());
				ShowAll();
			});

		Add(new Box(Orientation.Vertical, 8)
			.Prop(w => w.Expand = true)
			.Prop(w => w.Halign = Align.Fill)
			.Prop(w => w.Valign = Align.Fill)
			.AddClass("notifications-history__container")
			.AddMany(new Label("Notifications")
				.Prop(w => w.Halign = Align.Start)
				.AddClass("notifications-history__header"))
			.AddMany(new ScrolledWindow()
				.Prop(w => w.HscrollbarPolicy = PolicyType.Never)
				.Prop(w => w.Expand = true)
				.AddMany(accordion)));

		viewModelObs.Connect();
		ShowAll();
	}

	public Widget CreateNotificationEntry(IGroupedObservable<NotificationEntryViewModel, NotificationEntryViewModel> obs)
	{
		var displayedTime = new Label();
		displayedTime.AddClass("notifications-history-item__displayed-time");
		displayedTime.Halign = Align.Fill;
		displayedTime.Ellipsize = EllipsizeMode.End;
		displayedTime.MaxWidthChars = 1;
		displayedTime.Hexpand = true;
		displayedTime.Xalign = 0;

		var summary = new Label();
		summary.AddClass("notifications-history-item__summary");
		summary.Halign = Align.Fill;
		summary.Ellipsize = EllipsizeMode.End;
		summary.MaxWidthChars = 1;
		summary.Hexpand = true;
		summary.Xalign = 0;
		summary.Yalign = 0;

		var body = new Label();
		body.AddClass("notifications-history-item__body");
		body.Ellipsize = EllipsizeMode.End;
		body.Lines = 5;
		body.LineWrap = true;
		body.LineWrapMode = WrapMode.Word;
		body.MaxWidthChars = 1;
		body.Xalign = 0;
		body.Yalign = 0;

		var eventBox = new EventBox()
			.AddButtonStates()
			.AddClass("button")
			.Prop(w => w.ObserveButtonRelease().Subscribe(_ => _notificationsService.RemoveHistoryItem(obs.Key.Id)))
			.AddMany(new Box(Orientation.Vertical, 0)
				.AddClass("notifications-history-item__container")
				.AddMany(new Box(Orientation.Horizontal, 4)
					.AddMany(displayedTime)
					.AddMany(new Button()
						.AddButtonStates()
						.Prop(w => w.ObserveButtonRelease().Subscribe(_ => _notificationsService.RemoveHistoryItem(obs.Key.Id)))
						.Prop(w => w.Image = new Image { IconName = "window-close-symbolic", PixelSize = 16 })
						.Prop(w => w.Halign = Align.End)))
				.AddMany(new Box(Orientation.Horizontal, 0)
					.AddMany(new Image()
						.AddClass("notifications-history-item__image")
						.BindViewModel(obs.Select(s => s.Image).DistinctUntilChanged(), 34)
						.Prop(w => w.Xalign = 0)
						.Prop(w => w.Yalign = 0))
					.AddMany(new Box(Orientation.Vertical, 0)
						.AddMany(summary)
						.AddMany(body))));

		obs.Subscribe(n =>
		{
			displayedTime.Text = n.CreationDate.ToString("g");
			summary.Text = n.Summary;
			body.Text = n.Body;
			body.Visible = !string.IsNullOrEmpty(n.Body);
		});

		eventBox.ShowAll();

		return eventBox;
	}
}
