using Cairo;
using Gdk;
using Gtk;
using Window = Gtk.Window;

namespace GtkNetPanel;

public class SharpPanel : Window
{
	private CssProvider _cssProvider;

	public SharpPanel() : base("Null")
	{
		Decorated = false;
		Resizable = false;
		CanFocus = false;
		TypeHint = WindowTypeHint.Dock;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		var appMenuWidget = new AppMenu();
		appMenuWidget.SetSizeRequest(52, 52);

		_cssProvider = new CssProvider();
		_cssProvider.LoadFromData(@"
			label {
				font: 12px Sans;
			}
		");

		StyleContext.AddProvider(_cssProvider, int.MaxValue);

		var box = new Box(Orientation.Horizontal, 4);
		box.PackStart(appMenuWidget, false, false, 0);
		box.PackStart(new DrawingArea(), true, false, 4);
		box.PackStart(new SystemTray(), false, false, 4);
		box.PackStart(CreateClock(), false, false, 5);
		box.PackStart(new DrawingArea(), false, false, 4);
		Add(box);

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

	protected override bool OnDrawn(Context cr)
	{
		cr.Save();
		cr.SetSourceRGBA(0.4, 0.4, 0.4, 0.5);
		cr.Operator = Operator.Source;
		cr.Rectangle(0, 0, WidthRequest, HeightRequest);
		cr.Fill();
		cr.Restore();
		return base.OnDrawn(cr);
	}

	private Widget CreateClock()
	{
		var clockFormat = "HH:mm:ss\ndddd\nM/d/yyyy";
		var clock = new Label(DateTime.Now.ToString(clockFormat));
		clock.StyleContext.AddProvider(_cssProvider, uint.MaxValue);
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

	public void ReserveSpace()
	{
		var defaultDisplay = Display.Default;
		var monitor = defaultDisplay.GetMonitorAtWindow(Window);
		var monitorDimensions = monitor.Geometry;
		var reservedSpaceLong = new long[] { 0, 0, 0, 52, 0, 0, 0, 0, 0, 0, 0, monitorDimensions.Width }.SelectMany(BitConverter.GetBytes).ToArray();
		Property.Change(Window, Atom.Intern("_NET_WM_STRUT_PARTIAL", false), Atom.Intern("CARDINAL", false), 32, PropMode.Replace, reservedSpaceLong, 12);
	}
}
