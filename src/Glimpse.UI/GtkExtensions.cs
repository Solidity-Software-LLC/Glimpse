using System.Reactive.Linq;
using Cairo;
using Gdk;
using Glimpse.Common.Images;
using Glimpse.UI.State;
using Gtk;
using ReactiveMarbles.ObservableEvents;
using Monitor = Gdk.Monitor;
using Window = Gtk.Window;

namespace Glimpse.UI;

public static class GtkExtensions
{
	public static IEnumerable<Monitor> GetMonitors(this Display display)
	{
		for (var i = 0; i < display.NMonitors; i++)
		{
			yield return display.GetMonitor(i);
		}
	}

	public static bool Contains(this Monitor monitor, Gdk.Window window)
	{
		window.GetRootCoords(0, 0, out var x, out var y);
		return monitor.Geometry.Contains(x, y);
	}

	public static void RemoveAllChildren(this Container widget)
	{
		var widgets = widget.Children.ToList();
		widgets.ForEach(w => w.Destroy());
	}

	public static bool ContainsPoint(this Widget widget, int px, int py)
	{
		if (!widget.IsVisible) return false;
		widget.Window.GetGeometry(out var x, out var y, out var width, out var height);
		return px >= x && py >= y && px < x + width && py < y + height;
	}

	public static bool IsPointerInside(this Widget widget)
	{
		widget.Display.GetPointer(out var px, out var py);
		return widget.ContainsPoint(px, py);
	}

	public static void BindViewModel(this Image image, IObservable<ImageViewModel> imageViewModel, int size)
	{
		image.BindViewModel(imageViewModel, size, size);
	}

	private static void SetByName(this Image image, ImageViewModel vm, int width, int height)
	{
		if (vm.IconName.StartsWith("/"))
		{
			image.Pixbuf = new Pixbuf(vm.IconName).ScaleToFit(width, height);
		}
		else
		{
			image.SetFromIconName(vm.IconName, IconSize.LargeToolbar);
			image.PixelSize = width;
		}
	}

	private static void SetImage(this Image image, ImageViewModel vm, int width, int height)
	{
		image.Pixbuf = vm.Image.ScaleToFit(width, height).Image;
	}

	public static void BindViewModel(this Image image, IObservable<ImageViewModel> imageViewModel, int width, int height)
	{
		imageViewModel.Subscribe(vm =>
		{
			if (vm.Image != null) image.SetImage(vm, width, height);
			else if (!string.IsNullOrEmpty(vm.IconName)) image.SetByName(vm, width, height);
			else vm.IconName = Guid.NewGuid().ToString();
		});
	}

	public static void AppIcon(this Widget widget, Image image, IObservable<ImageViewModel> iconObservable, int size)
	{
		iconObservable.Subscribe(vm =>
		{
			if (vm.Image != null)
			{
				image.Data["Small"] = vm.Image.Scale(size - 6);
				image.Data["Big"] = vm.Image.Scale(size);
			}
			else if (vm.IconName.StartsWith("/"))
			{
				var glimpseImage = GlimpseImageFactory.From(new Pixbuf(vm.IconName, size, size));
				image.Pixbuf = glimpseImage.Image;
				image.Data["Small"] = glimpseImage.Scale(size - 6);
				image.Data["Big"] = glimpseImage;
			}
		});

		image.BindViewModel(iconObservable, size);
		widget.AddButtonStates();
		widget.ObserveEvent(w => w.Events().EnterNotifyEvent).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(w => w.Events().LeaveNotifyEvent).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(w => w.Events().ButtonPressEvent).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(w => w.Events().ButtonPressEvent).WithLatestFrom(iconObservable).Subscribe(t =>
		{
			if (image.Pixbuf == null) image.PixelSize = size - 6;
			else image.Pixbuf = ((GtkGlimpseImage)image.Data["Small"])?.Image;
		});
		widget.ObserveEvent(w => w.Events().ButtonReleaseEvent).WithLatestFrom(iconObservable).Subscribe(t =>
		{
			if (image.Pixbuf == null) image.PixelSize = size;
			else image.Pixbuf = ((GtkGlimpseImage)image.Data["Big"])?.Image;
		});
		widget.ObserveEvent(w => w.Events().LeaveNotifyEvent).WithLatestFrom(iconObservable).Subscribe(t =>
		{
			if (image.Pixbuf == null) image.PixelSize = size;
			else image.Pixbuf = ((GtkGlimpseImage)image.Data["Big"])?.Image;
		});
	}

	public static IObservable<ButtonReleaseEventArgs> ObserveButtonRelease(this Widget widget)
	{
		return widget.ObserveEvent(w => w.Events().ButtonReleaseEvent).TakeUntilDestroyed(widget);
	}

	public static Widget AddButtonStates(this Widget widget)
	{
		widget.AddEvents((int)(EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		widget.ObserveEvent(w => w.Events().EnterNotifyEvent).Subscribe(_ => widget.SetStateFlags(StateFlags.Prelight, true));
		widget.ObserveEvent(w => w.Events().LeaveNotifyEvent).Subscribe(_ => widget.SetStateFlags(StateFlags.Normal, true));
		widget.ObserveEvent(w => w.Events().ButtonPressEvent).Subscribe(_ => widget.SetStateFlags(StateFlags.Active, true));
		widget.ObserveEvent(w => w.Events().ButtonReleaseEvent).Subscribe(_ => widget.SetStateFlags(StateFlags.Prelight, true));
		return widget;
	}

	public static void CenterAbove(this Window window, Widget widget)
	{
		if (!window.Visible) return;

		widget.Window.GetRootCoords(0, 0, out var x, out var y);

		var windowX = x - window.Window.Width / 2 + widget.Window.Width / 2;
		var windowY = y - window.Window.Height - 16;

		window.Move(windowX, windowY);
	}

	public static void CenterOnScreenAboveWidget(this Window window, Widget widget)
	{
		if (!window.Visible) return;

		var monitor = window.Display.GetMonitorAtWindow(widget.Window);
		var monitorDimensions = monitor.Geometry;

		widget.Window.GetRootCoords(0, 0, out _, out var y);

		var windowX = monitorDimensions.X + monitorDimensions.Width / 2 - window.Window.Width / 2;
		var windowY = y - window.Window.Height - 16;

		window.Move(windowX, windowY);
	}

	public static IObservable<T> TakeUntilDestroyed<T>(this IObservable<T> obs, Widget source)
	{
		return obs.TakeUntil(source.Events().Destroyed.Take(1));
	}

	public static IObservable<T> ObserveEvent<T>(this Widget widget, IObservable<T> obs)
	{
		return obs.TakeUntilDestroyed(widget);
	}

	public static IObservable<T> ObserveEvent<TWidget, T>(this TWidget widget, Func<TWidget, IObservable<T>> f) where TWidget : Widget
	{
		return f(widget).TakeUntilDestroyed(widget);
	}

	public static IObservable<bool> CreateContextMenuObservable(this Widget widget)
	{
		var buttonPressObs = widget.ObserveEvent(w => w.Events().ButtonReleaseEvent)
			.Where(e => e.Event.Button == 3 && e.Event.Type == EventType.ButtonRelease)
			.Do(e => e.RetVal = true)
			.Select(_ => true);

		var popupMenuObs = widget.ObserveEvent(w => w.Events().PopupMenu)
			.Select(e => true);

		return buttonPressObs.Merge(popupMenuObs);
	}

	public static T AddClass<T>(this T widget, params string[] classes) where T : Widget
	{
		foreach (var c in classes) widget.StyleContext.AddClass(c);
		return widget;
	}

	public static T RemoveClass<T>(this T widget, params string[] classes) where T : Widget
	{
		foreach (var c in classes) widget.StyleContext.RemoveClass(c);
		return widget;
	}

	public static T AddMany<T>(this T widget, params Widget[] children) where T : Container
	{
		foreach (var c in children) widget.Add(c);
		return widget;
	}

	public static void RoundedRectangle(this Context cr, int x, int y, int width, int height, int cornerRadius)
	{
		cr.RoundedRectangle(x, y, width, height, cornerRadius, cornerRadius, cornerRadius, cornerRadius);
	}

	public static void RoundedRectangle(this Context cr, int x, int y, int width, int height, int upperRightRadius, int lowerRightRadius, int lowerLeftRadius, int upperLeftRadius)
	{
		var degrees = Math.PI / 180.0;

		cr.NewSubPath();
		cr.Arc(x + width - upperRightRadius, y + upperRightRadius, upperRightRadius, -90 * degrees, 0 * degrees);
		cr.Arc(x + width - lowerRightRadius, y + height - lowerRightRadius, lowerRightRadius, 0 * degrees, 90 * degrees);
		cr.Arc(x + lowerLeftRadius, y + height - lowerLeftRadius, lowerLeftRadius, 90 * degrees, 180 * degrees);
		cr.Arc(x + upperLeftRadius, y + upperLeftRadius, upperLeftRadius, 180 * degrees, 270 * degrees);
		cr.ClosePath();
	}
}
