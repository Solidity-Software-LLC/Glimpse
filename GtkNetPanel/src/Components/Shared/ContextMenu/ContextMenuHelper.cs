using Gdk;
using GLib;
using Gtk;
using Global = Gtk.Global;

namespace GtkNetPanel.Components.Shared.ContextMenu;

public class ContextMenuHelper : IDisposable
{
	private readonly Widget _widget;
	private bool propagating; //Prevent reentry

	public ContextMenuHelper(Widget widget)
	{
		_widget = widget;
		widget.PopupMenu += Widget_PopupMenu;
		widget.ButtonPressEvent += Widget_ButtonPressEvent;
	}

	public event EventHandler<ContextMenuEventArgs> ContextMenu;

	[ConnectBefore]
	private void Widget_PopupMenu(object o, PopupMenuArgs args) => RaiseContextMenuEvent(args, (Widget)o, false);

	[ConnectBefore]
	private void Widget_ButtonPressEvent(object o, ButtonPressEventArgs args)
	{
		if (args.Event.Button == 3 && args.Event.Type == EventType.ButtonPress)
		{
			RaiseContextMenuEvent(args, (Widget)o, true);
		}
	}

	private void RaiseContextMenuEvent(SignalArgs signalArgs, Widget widget, bool rightClick)
	{
		if (!propagating)
		{
			//Propagate the event
			var evnt = Global.CurrentEvent;
			propagating = true;
			Global.PropagateEvent(widget, evnt);
			propagating = false;
			signalArgs.RetVal = true; //The widget already processed the event in the propagation

			//Raise the context menu event
			var args = new ContextMenuEventArgs(widget, rightClick);
			if (ContextMenu != null)
			{
				ContextMenu.Invoke(this, args);
			}
		}
	}

	public void Dispose()
	{
		_widget.PopupMenu -= Widget_PopupMenu;
		_widget.ButtonPressEvent -= Widget_ButtonPressEvent;
	}
}
