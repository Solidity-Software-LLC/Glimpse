using GLib;
using GtkNetPanel.Components;
using Application = Gtk.Application;
using Display = Gdk.Display;

namespace GtkNetPanel;

public static class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);

		Application.Init();
		SynchronizationContext.SetSynchronizationContext(new GLibSynchronizationContext());

		var sharpPanel = new SharpPanel();

		var app = new Application("org.SharpPanel", ApplicationFlags.None);
		app.Register(Cancellable.Current);
		app.AddWindow(sharpPanel);

		sharpPanel.ShowAll();

		var defaultDisplay = Display.Default;
		var monitor = defaultDisplay.GetMonitorAtWindow(sharpPanel.Window);
		var monitorDimensions = monitor.Geometry;

		sharpPanel.ReserveSpace();
		sharpPanel.SetSizeRequest(monitorDimensions.Width, 52);
		sharpPanel.Move(0, monitorDimensions.Height - 52);

		try
		{
			Application.Run();
			return 0;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return -1;
		}
	}
}
