using System.Collections.Immutable;
using System.Reactive.Linq;
using Gdk;
using Gtk;
using GtkNetPanel.Services;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;
using Pango;
using Application = Gtk.Application;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarView : Box
{
	private readonly IObservable<ApplicationBarViewModel> _viewModelObservable;
	private readonly Application _application;
	private readonly ApplicationBarController _controller;
	private ImmutableDictionary<string, Widget> _icons = ImmutableDictionary<string, Widget>.Empty;

	public ApplicationBarView(IObservable<ApplicationBarViewModel> viewModelObservable, Application application, ApplicationBarController controller)
	{
		_viewModelObservable = viewModelObservable;
		_application = application;
		_controller = controller;

		viewModelObservable.Select(v => v.Tasks).DistinctUntilChanged().Subscribe(tasks =>
		{
			foreach (var pairing in _icons.Diff(tasks))
			{
				if (pairing.Left == null)
				{
					var icon = CreateAppIcon(pairing.Right);
					_icons = _icons.Add(pairing.Key, icon);
					PackStart(icon, false, false, 2);
				}
				else if (pairing.Right == null)
				{
					var icon = _icons[pairing.Key];
					Remove(icon);
					_icons = _icons.Remove(pairing.Key);
				}
			}

			ShowAll();
		});
	}

	private void CreateWindowPickerPopup(TaskState task, Widget applicationIcon)
	{
		var previewSelectionLayout = new Box(Orientation.Horizontal, 0);
		previewSelectionLayout.StyleContext.AddClass("app-preview-popup-container");

		// foreach (var matchingWindow in Window.Display.DefaultScreen.WindowStack.Where(w => w.GetIntProperty(Atoms._NET_WM_PID) == task.WindowId))
		// {
		// 	var preview = CreateAppPreview(matchingWindow);
		// 	previewSelectionLayout.PackStart(preview, false, false, 0);
		// }

		Window.GetOrigin(out _, out var y);
		applicationIcon.TranslateCoordinates(Toplevel, 0, 0, out var x, out _);

		var childWindow = new Gtk.Window(WindowType.Toplevel);
		childWindow.SkipPagerHint = true;
		childWindow.SkipTaskbarHint = true;
		childWindow.Decorated = false;
		childWindow.Resizable = false;
		childWindow.CanFocus = true;
		childWindow.AppPaintable = true;
		childWindow.Visual = Screen.RgbaVisual;
		childWindow.Add(previewSelectionLayout);
		childWindow.Move(x, y - childWindow.HeightRequest);
		childWindow.ShowAll();
		childWindow.FocusOutEvent += (_, _) => childWindow.Destroy();
		_application.AddWindow(childWindow);
		childWindow.GrabFocus();
	}

	// private Widget CreateAppPreview(GenericWindowRef windowRef)
	// {
	// 	var applicationName = new Label(matchingWindow.GetStringProperty(Atoms._NET_WM_NAME));
	// 	applicationName.Ellipsize = EllipsizeMode.Start;
	// 	applicationName.Justify = Justification.Left;
	//
	// 	var appWindowContainer = new Box(Orientation.Vertical, 0);
	// 	appWindowContainer.Hexpand = false;
	// 	appWindowContainer.Vexpand = false;
	// 	appWindowContainer.Add(applicationName);
	// 	appWindowContainer.Add(new Image(matchingWindow.GetWindowScreenshot(150)));
	//
	// 	var eventBox = new EventBox();
	// 	eventBox.AddHoverHighlighting();
	// 	eventBox.ButtonReleaseEvent += (o, args) => _controller.OnPreviewWindowClicked(args);
	// 	eventBox.Add(appWindowContainer);
	// 	eventBox.StyleContext.AddClass("app-preview-app");
	//
	// 	return eventBox;
	// }

	private EventBox CreateAppIcon(TaskState task)
	{
		var biggestIcon = task.Icons.MaxBy(i => i.Width);
		var imageBuffer = new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
		var image = new Image(imageBuffer.ScaleSimple(28, 28, InterpType.Bilinear));
		image.SetSizeRequest(42, 42);

		var eventBox = new EventBox();
		eventBox.Vexpand = false;
		eventBox.Valign = Align.Center;
		eventBox.StyleContext.AddClass("application-icon");
		eventBox.Add(image);
		eventBox.AddHoverHighlighting();
		eventBox.ButtonReleaseEvent += (o, args) => _controller.OnClickApplicationIcon(args, task);

		// This is the "ng-for" pattern
		var taskRemovedObservable = _viewModelObservable
			.Select(v => v.Tasks)
			.DistinctUntilChanged()
			.Where(tasks => !tasks.ContainsKey(task.WindowRef.Id))
			.Take(1);

		_viewModelObservable
			.TakeUntil(taskRemovedObservable)
			.Select(v => v.ShownWindowPicker)
			.DistinctUntilChanged()
			.Where(shownTask => shownTask?.WindowRef.Id == task.WindowRef.Id)
			.Subscribe(t =>
			{
				CreateWindowPickerPopup(t, eventBox);
			});

		return eventBox;
	}
}
