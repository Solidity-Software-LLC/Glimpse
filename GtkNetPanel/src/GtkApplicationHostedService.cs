using Autofac;
using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.Components;
using GtkNetPanel.Services.GtkSharp;
using Microsoft.Extensions.Hosting;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;

namespace GtkNetPanel;

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

				SynchronizationContext.SetSynchronizationContext(new GLibSynchronizationContext());
				Application.Init();
				_application.Register(Cancellable.Current);

				var assemblyName = typeof(GtkApplicationHostedService).Assembly.GetName().Name;
				var cssProvider = new CssProvider();
				cssProvider.LoadFromResource($"{assemblyName}.styles.css");
				StyleContext.AddProviderForScreen(Display.Default.DefaultScreen, cssProvider, uint.MaxValue);

				var panels = Display.Default
					.GetMonitors()
					.Select(m =>
					{
						var panel = _serviceProvider.BeginLifetimeScope("panel").Resolve<SharpPanel>();
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
