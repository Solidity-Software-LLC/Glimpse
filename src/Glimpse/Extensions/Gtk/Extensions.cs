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

	public static T AddButtonStates<T>(this T widget) where T : Widget
	{
		widget.AddEvents((int)(EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		widget.ObserveEvent(nameof(widget.EnterNotifyEvent)).Subscribe(_ => widget.SetStateFlags(StateFlags.Prelight, true));
		widget.ObserveEvent(nameof(widget.LeaveNotifyEvent)).Subscribe(_ => widget.SetStateFlags(StateFlags.Normal, true));
		widget.ObserveEvent(nameof(widget.ButtonPressEvent)).Subscribe(_ => widget.SetStateFlags(StateFlags.Active, true));
		widget.ObserveEvent(nameof(widget.ButtonReleaseEvent)).Subscribe(_ => widget.SetStateFlags(StateFlags.Prelight, true));
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
		return obs.TakeUntil(Observable.FromEventPattern(source, nameof(source.Destroyed)).Take(1));
	}

	public static IObservable<object> ObserveEvent(this Widget widget, string eventName)
	{
		return Observable.FromEventPattern<object>(widget, eventName).TakeUntilDestroyed(widget).Select(e => e.EventArgs);
	}

	public static IObservable<T> ObserveEvent<T>(this Widget widget, IObservable<T> obs)
	{
		return obs.TakeUntilDestroyed(widget);
	}

	public static IObservable<T> ObserveEvent<T>(this Widget widget, string eventName)
	{
		return Observable.FromEventPattern<T>(widget, eventName).TakeUntilDestroyed(widget).Select(e => e.EventArgs);
	}

	public static IObservable<bool> CreateContextMenuObservable(this Widget widget)
	{
		var buttonPressObs = widget.ObserveEvent<ButtonReleaseEventArgs>(nameof(widget.ButtonReleaseEvent))
			.Where(e => e.Event.Button == 3 && e.Event.Type == EventType.ButtonRelease)
			.Do(e => e.RetVal = true)
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

	public static double AspectRatio(this Pixbuf image)
	{
		return (double)image.Width / image.Height;
	}

	public static Pixbuf ScaleToFit(this Pixbuf imageBuffer, int maxHeight, int maxWidth)
	{
		var scaledWidth = maxHeight * imageBuffer.AspectRatio();
		var scaledHeight = (double) maxHeight;

		if (scaledWidth > maxWidth)
		{
			scaledWidth = maxWidth;
			scaledHeight /= imageBuffer.AspectRatio();
		}

		return imageBuffer.ScaleSimple((int) scaledWidth, (int) scaledHeight, InterpType.Bilinear);
	}

	public static Pixbuf Scale(this Pixbuf image, int size)
	{
		return image.ScaleSimple(size, size, InterpType.Bilinear);
	}

	public static void AppIcon(this Widget widget, Image image, IObservable<(Pixbuf BigIcon, Pixbuf SmallIcon)> iconObservable)
	{
		iconObservable.Subscribe(t => image.Pixbuf = t.Item1);
		widget.AddButtonStates();
		widget.ObserveEvent(nameof(widget.EnterNotifyEvent)).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(nameof(widget.LeaveNotifyEvent)).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(nameof(widget.ButtonPressEvent)).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(nameof(widget.ButtonPressEvent)).WithLatestFrom(iconObservable).Subscribe(t => image.Pixbuf = t.Second.SmallIcon);
		widget.ObserveEvent(nameof(widget.ButtonReleaseEvent)).WithLatestFrom(iconObservable).Subscribe(t => image.Pixbuf = t.Second.BigIcon);
	}
}
