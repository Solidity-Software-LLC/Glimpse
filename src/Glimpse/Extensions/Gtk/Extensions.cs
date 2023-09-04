using System.Reactive.Linq;
using Cairo;
using Gdk;
using Glimpse.State;
using Gtk;
using Window = Gtk.Window;

namespace Glimpse.Extensions.Gtk;

public static class Extensions
{
	public static IObservable<ButtonReleaseEventArgs> ObserveButtonRelease(this Widget widget)
	{
		return Observable.FromEventPattern<ButtonReleaseEventArgs>(widget, nameof(widget.ButtonReleaseEvent))
			.TakeUntilDestroyed(widget)
			.Select(e => e.EventArgs);
	}

	public static T AddHoverHighlighting<T>(this T widget) where T : Widget
	{
		widget.AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		widget.EnterNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Prelight, true);
		widget.LeaveNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Normal, true);
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

		var monitor = window.Display.GetMonitorAtWindow(window.Window);
		var monitorDimensions = monitor.Geometry;

		widget.Window.GetRootCoords(0, 0, out var x, out var y);

		var windowX = x + monitorDimensions.Width / 2 - window.Window.Width / 2;
		var windowY = y - window.Window.Height - 16;

		window.Move(windowX, windowY);
	}

	public static IObservable<T> TakeUntilDestroyed<T>(this IObservable<T> obs, Widget source)
	{
		return obs.TakeUntil(Observable.FromEventPattern(source, nameof(source.Destroyed)).Take(1));
	}

	public static IObservable<object> ObserveEvent(this Widget widget, string eventName)
	{
		return Observable.FromEventPattern<object>(widget, eventName).TakeUntilDestroyed(widget).Select(e => e.EventArgs);
	}

	public static IObservable<T> ObserveEvent<T>(this Widget widget, string eventName)
	{
		return Observable.FromEventPattern<T>(widget, eventName).TakeUntilDestroyed(widget).Select(e => e.EventArgs);
	}

	public static IObservable<bool> CreateContextMenuObservable(this Widget widget)
	{
		var buttonPressObs = widget.ObserveEvent<ButtonPressEventArgs>(nameof(widget.ButtonPressEvent))
			.Where(e => e.Event.Button == 3 && e.Event.Type == EventType.ButtonPress)
			.Select(_ => true);

		var popupMenuObs = widget.ObserveEvent<PopupMenuArgs>(nameof(widget.PopupMenu))
			.Select(e => true);

		return buttonPressObs.Merge(popupMenuObs);
	}

	public static T AddClass<T>(this T widget, params string[] classes) where T : Widget
	{
		foreach (var c in classes) widget.StyleContext.AddClass(c);
		return widget;
	}

	public static T AddMany<T>(this T widget, params Widget[] children) where T : Container
	{
		foreach (var c in children) widget.Add(c);
		return widget;
	}

	public static Pixbuf ToPixbuf(this BitmapImage image)
	{
		var surface = new ImageSurface(image.Data, image.Depth == 24 ? Format.RGB24 : Format.Argb32, image.Width, image.Height, 4 * image.Width);
		var buffer = new Pixbuf(surface, 0, 0, image.Width, image.Height);
		surface.Dispose();
		return buffer;
	}

	public static double AspectRatio(this BitmapImage image)
	{
		return (double)image.Width / image.Height;
	}

	public static Pixbuf ScaleToFit(this BitmapImage image, int maxHeight, int maxWidth)
	{
		var imageBuffer = image.ToPixbuf();
		var scaledWidth = maxHeight * image.AspectRatio();
		var scaledHeight = (double) maxHeight;

		if (scaledWidth > maxWidth)
		{
			scaledWidth = maxWidth;
			scaledHeight /= image.AspectRatio();
		}

		return imageBuffer.ScaleSimple((int) scaledWidth, (int) scaledHeight, InterpType.Bilinear);
	}
}
