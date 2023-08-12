using Gdk;
using Gtk;
using GtkNetPanel.Components.ApplicationBar;
using GtkNetPanel.Components.ApplicationMenuButton;
using GtkNetPanel.Components.SystemTray;
using Window = Gtk.Window;

namespace GtkNetPanel.Components;

public class SharpPanel : Window
{
	private const int PanelHeight = 52;

	public SharpPanel(SystemTrayBox systemTrayBox, ApplicationBarBox applicationBarBox) : base("Null")
	{
		Decorated = false;
		Resizable = false;
		CanFocus = false;
		TypeHint = WindowTypeHint.Dock;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		var box = new Box(Orientation.Horizontal, 0);
		box.Valign = Align.Center;
		box.Hexpand = true;
		box.Vexpand = false;
		box.PackStart(new AppMenu(), false, false, 0);
		box.PackStart(applicationBarBox, false, false, 0);
		box.PackStart(new DrawingArea(), true, false, 4);
		box.PackStart(systemTrayBox, false, false, 4);
		box.PackStart(CreateClock(), false, false, 5);
		box.PackStart(new DrawingArea(), false, false, 4);

		var wrapperBox = new Box(Orientation.Horizontal, 0);
		wrapperBox.Hexpand = true;
		wrapperBox.Expand = true;
		wrapperBox.Add(box);
		wrapperBox.StyleContext.AddClass("panel");
		Add(wrapperBox);

		ShowAll();

		// var helper = new ContextMenuHelper();
		// helper.AttachToWidget(this);
		// helper.ContextMenu += (o, a) =>
		// {
		// 	var popup = new Menu();
		// 	popup.Add(new MenuItem("Configure Panel"));
		// 	popup.Add(new MenuItem("Restart Panel"));
		// 	popup.Add(new SeparatorMenuItem());
		// 	popup.Add(new MenuItem("Remove Widget"));
		// 	popup.ShowAll();
		// 	popup.Popup();
		// };
	}

	private Widget CreateClock()
	{
		var clockFormat = "h:mm tt\ndddd\nM/d/yyyy";
		var clock = new Label(DateTime.Now.ToString(clockFormat));
		clock.Justify = Justification.Center;
		var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

		Task.Run(async () =>
		{
			while (await timer.WaitForNextTickAsync())
			{
				clock.Text = DateTime.Now.ToString(clockFormat);
				clock.QueueDraw();
			}
		});

		return clock;
	}

	public void DockToBottom(Gdk.Monitor monitor)
	{
		var monitorDimensions = monitor.Geometry;
		SetSizeRequest(monitorDimensions.Width, PanelHeight);
		Move(monitor.Workarea.Left, monitorDimensions.Height - PanelHeight);
		ReserveSpace(monitor);
	}

	private void ReserveSpace(Gdk.Monitor monitor)
	{
		var monitorDimensions = monitor.Geometry;
		var bottomStartX = monitor.Workarea.Left;
		var bottomEndX = bottomStartX + monitorDimensions.Width;
		var reservedSpaceLong = new long[] { 0, 0, 0, PanelHeight, 0, 0, 0, 0, 0, 0, bottomStartX, bottomEndX }.SelectMany(BitConverter.GetBytes).ToArray();
		Property.Change(Window, Atom.Intern("_NET_WM_STRUT_PARTIAL", false), Atom.Intern("CARDINAL", false), 32, PropMode.Replace, reservedSpaceLong, 12);
	}
}
