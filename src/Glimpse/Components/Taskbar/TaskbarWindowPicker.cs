using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.DisplayServer;
using Glimpse.State;
using Gtk;
using Pango;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.Components.Taskbar;

public class TaskbarWindowPicker : Window
{
	private readonly Subject<IWindowRef> _previewWindowClicked = new();
	private readonly Subject<IWindowRef> _closeWindow = new();

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

		viewModelObservable.Select(vm => vm.Tasks).UnbundleMany(t => t.WindowRef.Id).Subscribe(taskObservable =>
		{
			var preview = CreateAppPreview(taskObservable);
			layout.Add(preview);
			taskObservable.DistinctUntilChanged().Subscribe(_ => { }, _ => { }, () => layout.Remove(preview));
		});
	}

	public IObservable<IWindowRef> PreviewWindowClicked => _previewWindowClicked;
	public IObservable<IWindowRef> CloseWindow => _closeWindow;

	public void Popup()
	{
		Visible = true;
		ShowAll();
	}

	private Widget CreateAppPreview(IObservable<TaskState> taskObservable)
	{
		var appName = new Label()
			{
				Ellipsize = EllipsizeMode.End,
				Justify = Justification.Left,
				Halign = Align.Start,
				MaxWidthChars = 15,
				Expand = true
			}
			.AddClass("window-picker__app-name");

		var appIcon = new Image() { Halign = Align.Start }
			.AddClass("window-picker__app-icon");

		var closeIconBox = new Button() { Halign = Align.End }
			.AddClass("window-picker__app-close-button")
			.AddMany(new Image(Assets.Close.ScaleSimple(12, 12, InterpType.Bilinear)));

		var screenshotImage = new Image() { Halign = Align.Center }
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

		taskObservable.Select(t => t.Title).DistinctUntilChanged().Subscribe(t => appName.Text = t);
		taskObservable.Subscribe(t => appIcon.Pixbuf = IconLoader.LoadIcon(t.DesktopFile.IconName, 16) ?? IconLoader.LoadIcon(t, 16) ?? IconLoader.DefaultAppIcon(16));
		closeIconBox.ObserveButtonRelease().WithLatestFrom(taskObservable).Subscribe(t => _closeWindow.OnNext(t.Second.WindowRef));
		taskObservable.Select(t => t.Screenshot).DistinctUntilChanged().Subscribe(s => screenshotImage.Pixbuf = s.ScaleToFit(100, 200));
		appPreview.ObserveButtonRelease().WithLatestFrom(taskObservable).Subscribe(t => _previewWindowClicked.OnNext(t.Second.WindowRef));

		return appPreview;
	}
}
