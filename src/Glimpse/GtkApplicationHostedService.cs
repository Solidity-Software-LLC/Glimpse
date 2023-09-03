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

				var screenCss = new CssProvider();
				screenCss.LoadFromData(allCss.ToString());
				StyleContext.AddProviderForScreen(Display.Default.DefaultScreen, screenCss, uint.MaxValue);

				var panels = Display.Default
					.GetMonitors()
					.Select(m =>
					{
						var panel = _serviceProvider.BeginLifetimeScope("panel").Resolve<App>();
						panel.DockToBottom(m);
						return panel;
					})
					.ToList();

				panels.ForEach(_application.AddWindow);
				Application.Run();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}, cancellationToken);

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Application.Quit();
		return Task.CompletedTask;
	}
}
