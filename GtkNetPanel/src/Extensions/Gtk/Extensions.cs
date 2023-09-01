using System.Reactive.Linq;
using Gdk;
using Gtk;
using Window = Gtk.Window;

namespace GtkNetPanel.Extensions.Gtk;

public static class Extensions
{
	public static IObservable<ButtonReleaseEventArgs> CreateButtonReleaseObservable(this Widget widget)
	{
		return Observable.FromEventPattern<ButtonReleaseEventArgs>(widget, nameof(widget.ButtonReleaseEvent))
			.TakeUntilDestroyed(widget)
			.Select(e => e.EventArgs);
	}

	public static void AddHoverHighlighting(this Widget widget)
	{
		widget.AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		widget.EnterNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Prelight, true);
		widget.LeaveNotifyEvent += (_, _) => widget.SetStateFlags(StateFlags.Normal, true);
	}

	public static void CenterAbove(this Window window, Widget widget)
	{
		if (!window.Visible) return;

		widget.Window.GetOrigin(out _, out var y);
		widget.TranslateCoordinates(widget.Toplevel, 0, 0, out var x, out _);

		var windowX = x + widget.Window.Width / 2 - window.Window.Width / 2;
		var windowY = y - window.Window.Height - 16;
		if (windowX < 8) windowX = 8;

		window.Move(windowX, windowY);
	}

	public static void CenterOnScreenAboveWidget(this Window window, Widget widget)
	{
		if (!window.Visible) return;

		var monitor = window.Display.GetMonitorAtWindow(window.Window);
		var monitorDimensions = monitor.Geometry;

		widget.Window.GetOrigin(out _, out var y);

		var windowX = monitorDimensions.Width / 2 - window.Window.Width / 2;
		var windowY = y - window.Window.Height - 16;

		window.Move(windowX, windowY);
	}

	public static IObservable<T> TakeUntilDestroyed<T>(this IObservable<T> obs, Widget source)
	{
		return obs.TakeUntil(Observable.FromEventPattern(source, nameof(source.Destroyed)).Take(1));
	}

	public static IObservable<bool> CreateContextMenuObservable(this Widget widget)
	{
		var buttonPressObs = Observable.FromEventPattern<ButtonPressEventArgs>(widget, nameof(widget.ButtonPressEvent))
			.TakeUntilDestroyed(widget)
			.Where(e => e.EventArgs.Event.Button == 3 && e.EventArgs.Event.Type == EventType.ButtonPress)
			.Select(e => true);

		var popupMenuObs = Observable.FromEventPattern<PopupMenuArgs>(widget, nameof(widget.PopupMenu))
			.TakeUntilDestroyed(widget)
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
}
