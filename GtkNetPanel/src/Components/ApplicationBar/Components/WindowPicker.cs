using System.Reactive.Subjects;
using Gdk;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services;
using GtkNetPanel.State;
using Pango;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.ApplicationBar.Components;

public class WindowPicker : Window
{
	private readonly Application _application;
	private readonly Subject<GenericWindowRef> _previewWindowClicked = new();

	public WindowPicker(IObservable<ApplicationBarGroupViewModel> viewModelObservable, Application application) : base(WindowType.Toplevel)
	{
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = true;
		AppPaintable = true;
		TypeHint = WindowTypeHint.Dialog;
		Visual = Screen.RgbaVisual;

		_application = application;
		var previewSelectionLayout = new Box(Orientation.Horizontal, 0);
		previewSelectionLayout.AppPaintable = true;
		previewSelectionLayout.StyleContext.AddClass("app-preview-popup-container");

		Add(previewSelectionLayout);

		FocusOutEvent += (_, _) =>
		{
			application.RemoveWindow(this);
			Visible = false;
		};

		viewModelObservable.Subscribe(vm =>
		{
			previewSelectionLayout.Children.ToList().ForEach(previewSelectionLayout.Remove);

			foreach (var task in vm.Tasks)
			{
				var preview = CreateAppPreview(task);
				previewSelectionLayout.PackStart(preview, false, false, 0);
			}
		});
	}

	public IObservable<GenericWindowRef> PreviewWindowClicked => _previewWindowClicked;

	public void Popup()
	{
		Visible = true;
		ShowAll();
		_application.AddWindow(this);
		GrabFocus();
	}

	private Widget CreateAppPreview(TaskState task)
	{
		var applicationName = new Label(task.Title);
		applicationName.Ellipsize = EllipsizeMode.End;
		applicationName.Justify = Justification.Left;
		applicationName.MaxWidthChars = 1;

		var bitmapImage = task.Screenshot;
		var imageBuffer = new Pixbuf(bitmapImage.Data, Colorspace.Rgb, true, 8, bitmapImage.Width, bitmapImage.Height, 4 * bitmapImage.Width);
		var ratio = (double) imageBuffer.Width / imageBuffer.Height;
		imageBuffer = imageBuffer.ScaleSimple((int) (100 * ratio), 100, InterpType.Bilinear);

		var appWindowContainer = new Box(Orientation.Vertical, 0);
		appWindowContainer.Hexpand = false;
		appWindowContainer.Vexpand = false;
		appWindowContainer.Add(applicationName);
		appWindowContainer.Add(new Image(imageBuffer));
		appWindowContainer.StyleContext.AddClass("app-preview-layout");
		appWindowContainer.WidthRequest = 230;

		var eventBox = new EventBox();
		eventBox.AddHoverHighlighting();
		eventBox.ButtonReleaseEvent += (o, args) => _previewWindowClicked.OnNext(task.WindowRef);
		eventBox.Add(appWindowContainer);
		eventBox.StyleContext.AddClass("app-preview-app");

		return eventBox;
	}
}
