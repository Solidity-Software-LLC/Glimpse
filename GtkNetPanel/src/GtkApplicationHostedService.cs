using Gdk;
using GLib;
using GtkNetPanel.Components;
using GtkNetPanel.Services.GtkSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application = Gtk.Application;
using Task = System.Threading.Tasks.Task;

namespace GtkNetPanel;

public class GtkApplicationHostedService : IHostedService
{
	private readonly IServiceProvider _serviceProvider;

	public GtkApplicationHostedService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
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
				var app = new Application("org.SharpPanel", ApplicationFlags.None);
				app.Register(Cancellable.Current);

				var panels = Display.Default
					.GetMonitors()
					.Select(m =>
					{
						var panel = _serviceProvider.GetRequiredService<SharpPanel>();
						panel.DockToBottom(m);
						return panel;
					})
					.ToList();


				panels.ForEach(app.AddWindow);

				Application.Run();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}, cancellationToken);

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
