using System.Reactive.Subjects;
using Gdk;
using Gtk;
using GtkNetPanel.Extensions.Gtk;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.State;
using Pango;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.Taskbar;

public class TaskbarWindowPicker : Window
{
	private readonly Subject<GenericWindowRef> _previewWindowClicked = new();
	private readonly Subject<GenericWindowRef> _closeWindow = new();

	public TaskbarWindowPicker(IObservable<ApplicationBarGroupViewModel> viewModelObservable) : base(WindowType.Toplevel)
	{
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = true;
		AppPaintable = true;
		TypeHint = WindowTypeHint.Dialog;
		Visual = Screen.RgbaVisual;

		var previewSelectionLayout = new Box(Orientation.Horizontal, 0);
		previewSelectionLayout.AppPaintable = true;
		previewSelectionLayout.StyleContext.AddClass("app-preview-popup-container");

		Add(previewSelectionLayout);

		FocusOutEvent += (_, _) =>
		{
			Visible = false;
		};

		viewModelObservable.Subscribe(vm =>
		{
			previewSelectionLayout.Children.ToList().ForEach(previewSelectionLayout.Remove);

			foreach (var task in vm.Tasks)
			{
				var preview = CreateAppPreview(vm, task);
				previewSelectionLayout.PackStart(preview, false, false, 0);
			}
		});
	}

	public IObservable<GenericWindowRef> PreviewWindowClicked => _previewWindowClicked;
	public IObservable<GenericWindowRef> CloseWindow => _closeWindow;

	public void Popup()
	{
		Visible = true;
		ShowAll();
		GrabFocus();
	}

	private Widget CreateAppPreview(ApplicationBarGroupViewModel viewModel, TaskState task)
	{
		var icon = IconLoader.LoadIcon(viewModel.DesktopFile.IconName, 16)
			?? IconLoader.LoadIcon(task, 16)
			?? IconLoader.DefaultAppIcon(16);

		var appIcon = new Image(icon);

		var appName = new Label(task.Title);
		appName.Ellipsize = EllipsizeMode.End;
		appName.Justify = Justification.Left;
		appName.Halign = Align.Start;
		appName.Hexpand = true;
		appName.MaxWidthChars = 20;

		var closeIconBox = new EventBox();
		closeIconBox.AddHoverHighlighting();
		closeIconBox.StyleContext.AddClass("app-preview-close-icon");
		closeIconBox.SetSizeRequest(16, 16);
		closeIconBox.Add(new Image(Assets.Close.ScaleSimple(12, 12, InterpType.Bilinear)));
		closeIconBox.ButtonReleaseEvent += (_, _) => _closeWindow.OnNext(task.WindowRef);

		var topRow = new Box(Orientation.Horizontal, 4);
		topRow.PackStart(appIcon, false, false, 0);
		topRow.PackStart(appName, true, true, 0);
		topRow.PackStart(closeIconBox, false, false, 0);

		var bitmapImage = task.Screenshot;
		var imageBuffer = new Pixbuf(bitmapImage.Data, Colorspace.Rgb, true, 8, bitmapImage.Width, bitmapImage.Height, 4 * bitmapImage.Width);
		var ratio = (double) imageBuffer.Width / imageBuffer.Height;
		imageBuffer = imageBuffer.ScaleSimple((int) (100 * ratio), 100, InterpType.Bilinear);

		var appWindowContainer = new Box(Orientation.Vertical, 4);
		appWindowContainer.Hexpand = false;
		appWindowContainer.Vexpand = false;
		appWindowContainer.Add(topRow);
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
