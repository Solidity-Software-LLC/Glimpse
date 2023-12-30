using System.Reactive.Linq;
using Gdk;
using Gtk;
using ReactiveMarbles.ObservableEvents;
using Window = Gtk.Window;

namespace Glimpse.Common.Gtk;

public static class MoreGtkExtensions
{
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
}
