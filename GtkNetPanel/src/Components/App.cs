using Gdk;
using Gtk;
using GtkNetPanel.Components.ApplicationBar;
using GtkNetPanel.Components.ApplicationMenuButton;
using GtkNetPanel.Components.SystemTray;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace GtkNetPanel.Components;

public class App : Window
{
	private const int PanelHeight = 52;

	public App(SystemTrayBox systemTrayBox, ApplicationBarView applicationBarView) : base(WindowType.Toplevel)
	{
		Decorated = false;
		Resizable = false;
		TypeHint = WindowTypeHint.Dock;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		ButtonReleaseEvent += (_, _) => Window.Focus(0);

		var centerBox = new Box(Orientation.Horizontal, 0);
		centerBox.PackStart(new AppMenu(), false, false, 0);
		centerBox.PackStart(applicationBarView, false, false, 0);
		centerBox.Halign = Align.Center;

		var rightBox = new Box(Orientation.Horizontal, 0);
		rightBox.PackStart(systemTrayBox, false, false, 4);
		rightBox.PackStart(CreateClock(), false, false, 5);
		rightBox.Halign = Align.End;

		var grid = new Grid();
		grid.Attach(new DrawingArea(), 0, 0, 1, 1);
		grid.Attach(centerBox, 1, 0, 1, 1);
		grid.Attach(rightBox, 2, 0, 1, 1);
		grid.Vexpand = true;
		grid.Hexpand = true;
		grid.RowHomogeneous = true;
		grid.ColumnHomogeneous = true;
		grid.StyleContext.AddClass("panel");
		Add(grid);
		ShowAll();

		// var helper = new ContextMenuHelper(this);
		// helper.ContextMenu += (o, a) =>
		// {
		// 	var popup = new Menu();
		// 	popup.Add(new MenuItem("Open Task Manager"));
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
