using System.Reactive.Subjects;
using Gdk;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.DisplayServer;
using Glimpse.State;
using Gtk;
using Pango;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.Components.Taskbar;

public class TaskbarWindowPicker : Window
{
	private readonly Subject<GenericWindowRef> _previewWindowClicked = new();
	private readonly Subject<GenericWindowRef> _closeWindow = new();

	public TaskbarWindowPicker(IObservable<TaskbarGroupViewModel> viewModelObservable) : base(WindowType.Toplevel)
	{
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = true;
		TypeHint = WindowTypeHint.Dialog;
		Visual = Screen.RgbaVisual;

		var layout = new Box(Orientation.Horizontal, 0);

		Add(layout);
		this.ObserveEvent(nameof(FocusOutEvent)).Subscribe(_ => Visible = false);

		viewModelObservable.Subscribe(vm =>
		{
			layout.RemoveAllChildren();

			foreach (var task in vm.Tasks)
			{
				var preview = CreateAppPreview(vm, task);
				layout.PackStart(preview, false, false, 0);
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

	private Widget CreateAppPreview(TaskbarGroupViewModel viewModel, TaskState task)
	{
		var icon = IconLoader.LoadIcon(viewModel.DesktopFile.IconName, 16)
			?? IconLoader.LoadIcon(task, 16)
			?? IconLoader.DefaultAppIcon(16);

		var appIcon = new Image(icon)
			{
				Halign = Align.Start
			}
			.AddClass("window-picker__app-icon");

		var appName = new Label(task.Title)
			{
				Ellipsize = EllipsizeMode.End,
				Justify = Justification.Left,
				Halign = Align.Start,
				MaxWidthChars = 15,
				Expand = true
			}
			.AddClass("window-picker__app-name");

		var closeIconBox = new Button()
			{
				Halign = Align.End
			}
			.AddClass("window-picker__app-close-button")
			.AddMany(new Image(Assets.Close.ScaleSimple(12, 12, InterpType.Bilinear)));

		closeIconBox.ObserveButtonRelease().Subscribe(_ => _closeWindow.OnNext(task.WindowRef));

		var screenshotImage = new Image(task.Screenshot.ScaleToFit(100, 200))
			{
				Halign = Align.Center
			}
			.AddClass("window-picker__screenshot");

		var grid = new Grid();
		grid.Attach(appIcon, 0, 0, 1, 1);
		grid.AttachNextTo(appName, appIcon, PositionType.Right, 1, 1);
		grid.AttachNextTo(closeIconBox, appName, PositionType.Right, 1, 1);
		grid.Attach(screenshotImage, 0, 1, 3, 1);
		grid.AddClass("window-picker__app");

		var appPreview = new EventBox()
			.AddClass("window-picker__app-events")
			.AddMany(grid)
			.AddHoverHighlighting();

		appPreview.ObserveButtonRelease().Subscribe(_ => _previewWindowClicked.OnNext(task.WindowRef));

		return appPreview;
	}
}
