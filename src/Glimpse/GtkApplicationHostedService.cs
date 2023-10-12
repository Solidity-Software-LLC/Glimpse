using System.Reactive.Linq;
using System.Text;
using Autofac;
using Gdk;
using GLib;
using Glimpse.Components;
using Glimpse.Extensions.Gtk;
using Gtk;
using Microsoft.Extensions.Hosting;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;

namespace Glimpse;

public class GtkApplicationHostedService : IHostedService
{
	private readonly ILifetimeScope _serviceProvider;
	private readonly Application _application;

	public GtkApplicationHostedService(ILifetimeScope serviceProvider, Application application)
	{
		_serviceProvider = serviceProvider;
		_application = application;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Task.Run(() =>
		{
			try
			{
				ExceptionManager.UnhandledException += args =>
				{
					Console.WriteLine("Unhandled Exception:");
					Console.WriteLine(args.ExceptionObject);
					args.ExitApplication = false;
				};

				Application.Init();
				_application.Register(Cancellable.Current);

				var assembly = typeof(GtkApplicationHostedService).Assembly;
				var allCss = new StringBuilder();

				foreach (var cssFile in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".css")))
				{
					using var cssFileStream = new StreamReader(assembly.GetManifestResourceStream(cssFile));
					allCss.AppendLine(cssFileStream.ReadToEnd());
				}

				var display = Display.Default;
				var screenCss = new CssProvider();
				screenCss.LoadFromData(allCss.ToString());
				StyleContext.AddProviderForScreen(display.DefaultScreen, screenCss, uint.MaxValue);

				Observable.FromEventPattern<MonitorAddedArgs>(display, nameof(display.MonitorAdded)).Subscribe(_ => LoadPanels(display));
				Observable.FromEventPattern<MonitorRemovedArgs>(display, nameof(display.MonitorRemoved)).Subscribe(_ => LoadPanels(display));
				LoadPanels(display);
				Application.Run();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}, cancellationToken);

		return Task.CompletedTask;
	}

	private void LoadPanels(Display display)
	{
		var windows = _application.Windows.ToList();
		windows.ForEach(_application.RemoveWindow);

		var panels = display
			.GetMonitors()
			.Select(m =>
			{
				var panel = _serviceProvider.Resolve<Panel>();
				panel.DockToBottom(m);
				return panel;
			})
			.ToList();

		panels.ForEach(_application.AddWindow);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Application.Quit();
		return Task.CompletedTask;
	}
}
