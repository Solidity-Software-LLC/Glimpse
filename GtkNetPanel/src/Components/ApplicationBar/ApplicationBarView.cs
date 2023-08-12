using System.Reactive.Linq;
using Gdk;
using Gtk;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;
using Pango;
using Application = Gtk.Application;
using Window = Gdk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarView : Box
{
	private readonly IObservable<ApplicationBarViewModel> _viewModelObservable;
	private readonly Application _application;
	private readonly ApplicationBarController _controller;

	public ApplicationBarView(IObservable<ApplicationBarViewModel> viewModelObservable, Application application, ApplicationBarController controller)
	{
		_viewModelObservable = viewModelObservable;
		_application = application;
		_controller = controller;

		viewModelObservable.Select(v => v.Tasks).DistinctUntilChanged().Subscribe(tasks =>
		{
			foreach (var task in tasks)
			{
				PackStart(CreateAppIcon(task), false, false, 2);
			}

			ShowAll();
		});
	}

	private Pixbuf GetWindowScreenshot(Window window)
	{
		var ratio = (double) window.Width / window.Height;
		return new Pixbuf(window, 0, 0, window.Width, window.Height).ScaleSimple((int) (150 * ratio), 150, InterpType.Bilinear);
	}

	private void CreateWindowPickerPopup(TaskState task, Widget applicationIcon)
	{
		var matchingWindow = Window.Display.DefaultScreen.WindowStack.First(w => w.GetIntProperty(Atoms._NET_WM_PID) == task.ProcessId);

		Window.GetOrigin(out _, out var y);
		applicationIcon.TranslateCoordinates(Toplevel, 0, 0, out var x, out _);
		var childWindow = new Gtk.Window(WindowType.Toplevel);
		childWindow.SkipPagerHint = true;
		childWindow.SkipTaskbarHint = true;
		childWindow.Decorated = false;
		childWindow.Resizable = false;
		childWindow.Sensitive = false;

		var layoutBox = new Box(Orientation.Vertical, 5);
		layoutBox.Hexpand = false;
		var applicationName = new Label(matchingWindow.GetStringProperty(Atoms._NET_WM_NAME));
		applicationName.Ellipsize = EllipsizeMode.End;
		applicationName.Justify = Justification.Left;
		applicationName.SetSizeRequest(100, 20);
		layoutBox.Add(applicationName);
		layoutBox.Add(new Image(GetWindowScreenshot(matchingWindow)));

		childWindow.Add(layoutBox);
		childWindow.SetSizeRequest(200, 200);
		childWindow.Move(x, y - childWindow.HeightRequest);
		childWindow.ShowAll();
		childWindow.Deletable = true;

		childWindow.FocusOutEvent += (_, _) => childWindow.HideOnDelete();
		_application.AddWindow(childWindow);
		childWindow.GrabFocus();
	}

	private EventBox CreateAppIcon(TaskState task)
	{
		var biggestIcon = task.Icons.MaxBy(i => i.Width);
		var imageBuffer = new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
		var image = new Image(imageBuffer.ScaleSimple(28, 28, InterpType.Bilinear));
		image.SetSizeRequest(42, 42);

		var eventBox = new EventBox();
		eventBox.Vexpand = false;
		eventBox.Valign = Align.Center;
		eventBox.StyleContext.AddClass("highlight");
		eventBox.StyleContext.AddClass("application-icon");
		eventBox.AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		eventBox.Add(image);
		eventBox.EnterNotifyEvent += (_, _) => eventBox.SetStateFlags(StateFlags.Prelight, true);
		eventBox.LeaveNotifyEvent += (_, _) => eventBox.SetStateFlags(StateFlags.Normal, true);
		eventBox.ButtonReleaseEvent += (o, args) => _controller.OnClickApplicationIcon(args, task);

		var taskRemovedObservable = _viewModelObservable
			.Select(v => v.Tasks)
			.DistinctUntilChanged()
			.Where(tasks => tasks.All(t => t.ProcessId != task.ProcessId))
			.Take(1);

		_viewModelObservable
			.TakeUntil(taskRemovedObservable)
			.Select(v => v.ShownWindowPicker)
			.DistinctUntilChanged()
			.Where(shownTask => shownTask?.ProcessId == task.ProcessId)
			.Subscribe(t =>
			{
				CreateWindowPickerPopup(t, eventBox);
			});

		return eventBox;
	}
}
