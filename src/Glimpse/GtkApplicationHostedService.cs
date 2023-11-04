using System.Reactive.Linq;
using System.Text;
using Autofac;
using Fluxor;
using Gdk;
using GLib;
using Glimpse.Components;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.State;
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
	private readonly IState<RootState> _rootState;
	private readonly IDispatcher _dispatcher;

	public GtkApplicationHostedService(ILifetimeScope serviceProvider, Application application, IState<RootState> rootState, IDispatcher dispatcher)
	{
		_serviceProvider = serviceProvider;
		_application = application;
		_rootState = rootState;
		_dispatcher = dispatcher;
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

				var iconTheme = IconTheme.GetForScreen(screen);
				var iconThemeChangedObs = Observable.FromEventPattern(iconTheme, nameof(iconTheme.Changed));
				var desktopFilesObs = _rootState.ToObservable().Select(r => r.Entities.DesktopFiles).DistinctUntilChanged();

				desktopFilesObs.Merge(iconThemeChangedObs.WithLatestFrom(desktopFilesObs).Select(t => t.Second)).Subscribe(desktopFiles =>
				{
					var icons = desktopFiles.ById.Values
						.SelectMany(f => f.Actions.Select(a => a.IconName).Concat(new[] { f.IconName }))
						.Where(i => !string.IsNullOrEmpty(i))
						.Distinct()
						.ToDictionary(n => n, n => iconTheme.LoadIcon(n, 512));

					_dispatcher.Dispatch(new AddOrUpdateNamedIconsAction() { Icons = icons });
				});

				// Handle complete
				_rootState.ToObservable().Select(r => r.Entities.Windows.ById.Values).DistinctUntilChanged().UnbundleMany(g => g.WindowRef.Id).Subscribe(windowObs =>
				{
					windowObs.Select(g => g.Item1.IconName).DistinctUntilChanged().Subscribe(iconName =>
					{
						if (iconName == null) return;
						_dispatcher.Dispatch(new AddOrUpdateNamedIconsAction() { Icons = new Dictionary<string, Pixbuf>() { { iconName, iconTheme.LoadIcon(iconName, 512) } } });
					});
				});

				var action = (SimpleAction) _application.LookupAction("LoadPanels");
				Observable.FromEventPattern(action, nameof(action.Activated)).Subscribe(_ => LoadPanels(display));
				Observable.FromEventPattern(screen, nameof(screen.SizeChanged)).Subscribe(_ => LoadPanels(display));
				Observable.FromEventPattern(screen, nameof(screen.MonitorsChanged)).Subscribe(_ => LoadPanels(display));
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
