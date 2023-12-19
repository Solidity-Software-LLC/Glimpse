using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Glimpse.Components.Shared;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.DisplayServer;
using Gtk;
using Pango;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.Components.Taskbar;

public class TaskbarWindowPicker : Window
{
	private readonly Subject<IWindowRef> _previewWindowClicked = new();
	private readonly Subject<IWindowRef> _closeWindow = new();

	public TaskbarWindowPicker(IObservable<SlotViewModel> viewModelObservable) : base(WindowType.Popup)
	{
		SkipPagerHint = true;
		SkipTaskbarHint = true;
		Decorated = false;
		Resizable = false;
		CanFocus = true;
		TypeHint = WindowTypeHint.Dialog;
		Visual = Screen.RgbaVisual;
		KeepAbove = true;

		var layout = new Box(Orientation.Horizontal, 0);
		Add(layout);
		this.ObserveEvent(w => w.Events().FocusOutEvent).Subscribe(_ => ClosePopup());

		viewModelObservable.Select(vm => vm.Tasks).UnbundleMany(t => t.WindowRef.Id).RemoveIndex().Subscribe(taskObservable =>
		{
			var preview = CreateAppPreview(taskObservable);
			layout.Add(preview);
			taskObservable.DistinctUntilChanged().Subscribe(_ => { }, _ => { }, () => layout.Remove(preview));
		});
	}

	public IObservable<IWindowRef> PreviewWindowClicked => _previewWindowClicked;
	public IObservable<IWindowRef> CloseWindow => _closeWindow;

	public void ClosePopup()
	{
		Visible = false;
	}

	public void Popup()
	{
		ShowAll();
	}

	private Widget CreateAppPreview(IObservable<WindowViewModel> taskObservable)
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
			.AddButtonStates();

		taskObservable.Select(t => t.Title).DistinctUntilChanged().Subscribe(t => appName.Text = t);
		taskObservable.Subscribe(t =>
		{
			appIcon.Pixbuf?.Dispose();
			appIcon.Pixbuf = t.Icon.Scale(ThemeConstants.MenuItemIconSize);
		});
		closeIconBox.ObserveButtonRelease().WithLatestFrom(taskObservable).Subscribe(t => _closeWindow.OnNext(t.Second.WindowRef));
		taskObservable.Select(t => t.Screenshot).DistinctUntilChanged().Subscribe(s => screenshotImage.Pixbuf = s);
		appPreview.ObserveButtonRelease().WithLatestFrom(taskObservable).Subscribe(t => _previewWindowClicked.OnNext(t.Second.WindowRef));

		return appPreview;
	}
}
