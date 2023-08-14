using System.Reactive.Linq;
using Gdk;
using Gtk;
using GtkNetPanel.Services;
using GtkNetPanel.State;
using Pango;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components.ApplicationBar;

public class IconGroup
{
	public LinkedList<TaskState> Tasks { get; set; } = new();
	public Widget Widget { get; set; }
}

public class ApplicationBarView : Box
{
	private readonly IObservable<ApplicationBarViewModel> _viewModelObservable;
	private readonly Application _application;
	private readonly ApplicationBarController _controller;
	private readonly HashSet<string> _knownWindows = new();
	private readonly Dictionary<string, IconGroup> _groups = new();

	public ApplicationBarView(IObservable<ApplicationBarViewModel> viewModelObservable, Application application, ApplicationBarController controller)
	{
		_viewModelObservable = viewModelObservable;
		_application = application;
		_controller = controller;

		viewModelObservable.Select(v => v.Tasks).DistinctUntilChanged().Subscribe(tasks =>
		{
			foreach (var kv in tasks)
			{
				if (_knownWindows.Contains(kv.Key))
				{
					UpdateTask(kv);
				}
				else
				{
					AddTask(kv);
				}
			}

			foreach (var knownWindow in _knownWindows)
			{
				if (!tasks.ContainsKey(knownWindow))
				{
					RemoveTask(knownWindow);
				}
			}

			ShowAll();
		});
	}

	private void RemoveTask(string knownWindow)
	{
		_knownWindows.Remove(knownWindow);

		var group = _groups.First(g => g.Value.Tasks.Any(t => t.WindowRef.Id == knownWindow));
		var task = group.Value.Tasks.First(t => t.WindowRef.Id == knownWindow);

		if (group.Value.Tasks.Count == 1)
		{
			_groups.Remove(group.Key);
			Remove(group.Value.Widget);
		}
		else
		{
			group.Value.Tasks.Remove(task);

			if (group.Value.Tasks.Count == 1)
			{
				// Update icon
			}
		}
	}

	private void AddTask(KeyValuePair<string, TaskState> kv)
	{
		_knownWindows.Add(kv.Key);

		if (!_groups.ContainsKey(kv.Value.ApplicationName))
		{
			var newGroup = new IconGroup();
			newGroup.Tasks.AddLast(kv.Value);
			newGroup.Widget = CreateAppIcon(newGroup, kv.Value);
			_groups.Add(kv.Value.ApplicationName, newGroup);
			PackStart(newGroup.Widget, false, false, 0);
		}
		else
		{
			var group = _groups[kv.Value.ApplicationName];
			group.Tasks.AddLast(kv.Value);

			if (group.Tasks.Count == 2)
			{
				// Update icon
			}
		}

		ShowAll();
	}

	private void UpdateTask(KeyValuePair<string, TaskState> kv)
	{
		var group = _groups.First(g => g.Value.Tasks.Any(t => t.WindowRef.Id == kv.Key));
		var task = group.Value.Tasks.First(t => t.WindowRef.Id == kv.Key);
		var taskNode = group.Value.Tasks.Find(task);

		group.Value.Tasks.AddBefore(taskNode, kv.Value);
		group.Value.Tasks.Remove(task);
	}

	private void CreateWindowPickerPopup(IconGroup group)
	{
		var previewSelectionLayout = new Box(Orientation.Horizontal, 0);
		previewSelectionLayout.StyleContext.AddClass("app-preview-popup-container");

		foreach (var task in group.Tasks)
		{
			var preview = CreateAppPreview(task);
			previewSelectionLayout.PackStart(preview, false, false, 0);
		}

		Window.GetOrigin(out _, out var y);
		group.Widget.TranslateCoordinates(Toplevel, 0, 0, out var x, out _);

		var childWindow = new Window(WindowType.Toplevel);
		childWindow.SkipPagerHint = true;
		childWindow.SkipTaskbarHint = true;
		childWindow.Decorated = false;
		childWindow.Resizable = false;
		childWindow.CanFocus = true;
		childWindow.AppPaintable = true;
		childWindow.TypeHint = WindowTypeHint.Dialog;
		childWindow.Add(previewSelectionLayout);
		childWindow.Move(x, y - childWindow.HeightRequest);
		childWindow.ShowAll();
		childWindow.FocusOutEvent += (_, _) =>
		{

			childWindow.Destroy();
		};
		_application.AddWindow(childWindow);
		childWindow.GrabFocus();
	}

	private Widget CreateAppPreview(TaskState task)
	{
		var applicationName = new Label(task.Title);
		applicationName.Ellipsize = EllipsizeMode.Start;
		applicationName.Justify = Justification.Left;

		var bitmapImage = _controller.CaptureWindowScreenshot(task.WindowRef);
		var imageBuffer = new Pixbuf(bitmapImage.Data, Colorspace.Rgb, true, 8, bitmapImage.Width, bitmapImage.Height, 4 * bitmapImage.Width);
		imageBuffer = imageBuffer.ScaleSimple(150, 150, InterpType.Bilinear);

		var appWindowContainer = new Box(Orientation.Vertical, 0);
		appWindowContainer.Hexpand = false;
		appWindowContainer.Vexpand = false;
		appWindowContainer.Add(applicationName);
		appWindowContainer.Add(new Image(imageBuffer));

		var eventBox = new EventBox();
		eventBox.AddHoverHighlighting();
		eventBox.ButtonReleaseEvent += (o, args) => _controller.OnPreviewWindowClicked(args, task.WindowRef);
		eventBox.Add(appWindowContainer);
		eventBox.StyleContext.AddClass("app-preview-app");

		return eventBox;
	}

	private EventBox CreateAppIcon(IconGroup group, TaskState task)
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
			.Where(_ => _groups.Count == 0)
			.Take(1);

		_viewModelObservable
			.TakeUntil(taskRemovedObservable)
			.Select(v => v.ShownWindowPicker)
			.DistinctUntilChanged()
			.Where(shownTask => shownTask?.WindowRef.Id == task.WindowRef.Id)
			.Subscribe(t =>
			{
				CreateWindowPickerPopup(group);
			});

		return eventBox;
	}
}
