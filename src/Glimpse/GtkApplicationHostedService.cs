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
using Monitor = Gdk.Monitor;
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
				var screen = display.DefaultScreen;
				var screenCss = new CssProvider();
				screenCss.LoadFromData(allCss.ToString());
				StyleContext.AddProviderForScreen(screen, screenCss, uint.MaxValue);

				var action = (SimpleAction) _application.LookupAction("LoadPanels");
				Observable.FromEventPattern(action, nameof(action.Activated)).ObserveOn(new GLibSynchronizationContext()).Select(_ => true).Subscribe(_ =>
				{
					LoadPanels(display);
				});

				Observable.FromEventPattern<EventArgs>(screen, nameof(screen.SizeChanged)).Subscribe(_ => LoadPanels(display));
				Observable.FromEventPattern<EventArgs>(screen, nameof(screen.MonitorsChanged)).Subscribe(_ => LoadPanels(display));
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

	private List<Panel> _windows = new();

	private void LoadPanels(Display display)
	{
		_windows.ForEach(w =>
		{
			w.Close();
			w.Dispose();
		});

		_windows = display
			.GetMonitors()
			.Select(m => _serviceProvider.Resolve<Panel>(new TypedParameter(typeof(Monitor), m)))
			.ToList();

		_windows.ForEach(w => w.ShowAll());
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Application.Quit();
		return Task.CompletedTask;
	}
}
