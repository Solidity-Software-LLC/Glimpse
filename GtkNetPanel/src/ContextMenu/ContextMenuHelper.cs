using Gdk;
using GLib;
using Gtk;
using Global = Gtk.Global;

namespace GtkNetPanel;

public class ContextMenuHelper
{
    private bool propagating; //Prevent reentry

    public ContextMenuHelper() { }

    public ContextMenuHelper(Widget widget) => AttachToWidget(widget);

    public ContextMenuHelper(Widget widget, EventHandler<ContextMenuEventArgs> handler)
    {
        AttachToWidget(widget);
        ContextMenu += handler;
    }

    public event EventHandler<ContextMenuEventArgs> ContextMenu;

    public void AttachToWidget(Widget widget)
    {
        widget.PopupMenu += Widget_PopupMenu;
        widget.ButtonPressEvent += Widget_ButtonPressEvent;
    }

    public void DetachFromWidget(Widget widget)
    {
        widget.PopupMenu -= Widget_PopupMenu;
        widget.ButtonPressEvent -= Widget_ButtonPressEvent;
    }

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
            Event? evnt = Global.CurrentEvent;
            propagating = true;
            Global.PropagateEvent(widget, evnt);
            propagating = false;
            signalArgs.RetVal = true; //The widget already processed the event in the propagation

            //Raise the context menu event
            ContextMenuEventArgs args = new ContextMenuEventArgs(widget, rightClick);
            if (ContextMenu != null)
            {
                ContextMenu.Invoke(this, args);
            }
        }
    }
}
