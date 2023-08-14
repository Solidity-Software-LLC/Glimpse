using System.Reactive.Linq;
using Gdk;
using Gtk;
using GtkNetPanel.State;
using Pango;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarView : Box
{
	private readonly Application _application;
	private readonly ApplicationBarController _controller;
	private readonly Dictionary<string, Widget> _icons = new();

	public ApplicationBarView(IObservable<ApplicationBarViewModel> viewModelObservable, Application application, ApplicationBarController controller)
	{
		_application = application;
		_controller = controller;

		viewModelObservable.Select(vm => vm.GroupForWindowPicker).DistinctUntilChanged().Where(w => w != null).WithLatestFrom(viewModelObservable).Subscribe(tuple =>
		{
			var windowPickerPopup = CreateWindowPickerPopup(tuple.Second.Groups[tuple.First]);
			_application.AddWindow(windowPickerPopup);
			windowPickerPopup.GrabFocus();
		});

		viewModelObservable.Select(v => v.Groups).DistinctUntilChanged().SelectMany(g => g.Values).GroupBy(g => g.ApplicationName).Subscribe(groupedObservable =>
		{
			groupedObservable.Take(1).Subscribe(group =>
			{
				_icons.Add(group.ApplicationName, CreateAppIcon(group));
				PackStart(_icons[group.ApplicationName], false, false, 0);
				ShowAll();
			});

			groupedObservable.Skip(1).Subscribe(group => { }, // Update icon if it changed
			e => { },
			() =>
			{
				var widget = _icons[groupedObservable.Key];
				_icons.Remove(groupedObservable.Key);
				Remove(widget);
			});
		});
	}

	private Window CreateWindowPickerPopup(IconGroupViewModel group)
	{
		var applicationIcon = _icons[group.ApplicationName];
		var previewSelectionLayout = new Box(Orientation.Horizontal, 0);
		previewSelectionLayout.AppPaintable = true;
		previewSelectionLayout.StyleContext.AddClass("app-preview-popup-container");

		foreach (var task in group.Tasks)
		{
			var preview = CreateAppPreview(task);
			previewSelectionLayout.PackStart(preview, false, false, 0);
		}

		Window.GetOrigin(out _, out var y);
		applicationIcon.TranslateCoordinates(Toplevel, 0, 0, out var x, out _);

		var windowPickerPopup = new Window(WindowType.Toplevel);
		windowPickerPopup.SkipPagerHint = true;
		windowPickerPopup.SkipTaskbarHint = true;
		windowPickerPopup.Decorated = false;
		windowPickerPopup.Resizable = false;
		windowPickerPopup.CanFocus = true;
		windowPickerPopup.AppPaintable = true;
		windowPickerPopup.TypeHint = WindowTypeHint.Dialog;
		windowPickerPopup.Visual = Screen.RgbaVisual;
		windowPickerPopup.Add(previewSelectionLayout);
		windowPickerPopup.ShowAll();
		windowPickerPopup.Destroyed += (_, _) => windowPickerPopup.Dispose();
		windowPickerPopup.FocusOutEvent += (_, _) =>
		{
			_controller.CloseWindowPicker();
			windowPickerPopup.Close();
		};

		var hasMoved = false;

		windowPickerPopup.Drawn += (_, _) =>
		{
			if (hasMoved) return;
			hasMoved = true;
			var windowX = x + applicationIcon.Window.Width / 2 - windowPickerPopup.Window.Width / 2;
			var windowY = y - windowPickerPopup.Window.Height - 8;
			if (windowX < 8) windowX = 8;
			windowPickerPopup.Move(windowX, windowY);
		};

		return windowPickerPopup;
	}

	private Widget CreateAppPreview(TaskState task)
	{
		var applicationName = new Label(task.Title);
		applicationName.Ellipsize = EllipsizeMode.End;
		applicationName.Justify = Justification.Left;
		applicationName.MaxWidthChars = 1;

		var bitmapImage = _controller.CaptureWindowScreenshot(task.WindowRef);
		var imageBuffer = new Pixbuf(bitmapImage.Data, Colorspace.Rgb, true, 8, bitmapImage.Width, bitmapImage.Height, 4 * bitmapImage.Width);
		var ratio = (double) imageBuffer.Width / imageBuffer.Height;
		imageBuffer = imageBuffer.ScaleSimple((int) (100 * ratio), 100, InterpType.Bilinear);
		Console.WriteLine(125 * ratio);

		var appWindowContainer = new Box(Orientation.Vertical, 0);
		appWindowContainer.Hexpand = false;
		appWindowContainer.Vexpand = false;
		appWindowContainer.Add(applicationName);
		appWindowContainer.Add(new Image(imageBuffer));
		appWindowContainer.StyleContext.AddClass("app-preview-layout");
		appWindowContainer.WidthRequest = 230;

		var eventBox = new EventBox();
		eventBox.AddHoverHighlighting();
		eventBox.ButtonReleaseEvent += (o, args) => _controller.OnPreviewWindowClicked(args, task.WindowRef);
		eventBox.Add(appWindowContainer);
		eventBox.StyleContext.AddClass("app-preview-app");

		return eventBox;
	}

	private EventBox CreateAppIcon(IconGroupViewModel group)
	{
		var task = group.Tasks.First();
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
		eventBox.ButtonReleaseEvent += (o, args) => _controller.OnClickApplicationIcon(args, group.ApplicationName);

		return eventBox;
	}
}
