using System.Reactive.Linq;
using System.Text;
using Autofac;
using Gdk;
using GLib;
using Glimpse.Components;
using Glimpse.Components.Calendar;
using Glimpse.Components.StartMenu.Window;
using Glimpse.Components.Taskbar;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Extensions.Redux;
using Glimpse.State;
using Gtk;
using Application = Gtk.Application;
using Monitor = Gdk.Monitor;
using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;

namespace Glimpse;

public class GlimpseGtkApplication(
	ILifetimeScope serviceProvider,
	Application application,
	ReduxStore store)
{
	private List<Panel> _panels = new();

	public async Task InitializeAsync()
	{
		await Task.Yield();

		ExceptionManager.UnhandledException += args =>
		{
			Console.WriteLine("Unhandled Exception:");
			Console.WriteLine(args.ExceptionObject);
			args.ExitApplication = false;
		};

		store.Select(TaskbarSelectors.Slots)
			.Take(1)
			.Subscribe(slots => store.Dispatch(new UpdateTaskbarSlotOrderingBulkAction() { Slots = slots.Refs }));

		store.Select(TaskbarSelectors.SortedSlots)
			.DistinctUntilChanged()
			.Subscribe(slots => store.Dispatch(new UpdateTaskbarSlotOrderingBulkAction() { Slots = slots.Refs }));

		Application.Init();
		application.Register(Cancellable.Current);
		LoadCss();
		WatchIcons();

		var display = Display.Default;
		var screen = display.DefaultScreen;
		var action = (SimpleAction) application.LookupAction("LoadPanels");
		Observable.FromEventPattern(action, nameof(action.Activated)).Subscribe(_ => LoadPanels(display));
		Observable.FromEventPattern(screen, nameof(screen.SizeChanged)).Subscribe(_ => LoadPanels(display));
		Observable.FromEventPattern(screen, nameof(screen.MonitorsChanged)).Subscribe(_ => LoadPanels(display));
		application.AddWindow(serviceProvider.Resolve<StartMenuWindow>());
		application.AddWindow(serviceProvider.Resolve<CalendarWindow>());
		LoadPanels(display);
		Application.Run();
	}

	private void LoadCss()
	{
		var assembly = typeof(GlimpseHostedService).Assembly;
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
	}

	private void WatchIcons()
	{
		var display = Display.Default;
		var screen = display.DefaultScreen;
		var iconTheme = IconTheme.GetForScreen(screen);
		var iconThemeChangedObs = Observable.FromEventPattern(iconTheme, nameof(iconTheme.Changed));
		var desktopFilesObs = store.Select(RootStateSelectors.DesktopFiles).DistinctUntilChanged();

		desktopFilesObs.Merge(iconThemeChangedObs.WithLatestFrom(desktopFilesObs).Select(t => t.Second)).Subscribe(desktopFiles =>
		{
			var icons = desktopFiles.ById.Values
				.SelectMany(f => f.Actions.Select(a => a.IconName).Concat(new[] { f.IconName }))
				.Where(i => !string.IsNullOrEmpty(i))
				.Distinct()
				.ToDictionary(n => n, n => iconTheme.LoadIcon(n, 64));

			store.Dispatch(new AddOrUpdateNamedIconsAction() { Icons = icons });
		});

		// Handle complete
		store.Select(RootStateSelectors.Windows).Select(r => r.ById.Values).DistinctUntilChanged().UnbundleMany(g => g.WindowRef.Id).Subscribe(windowObs =>
		{
			windowObs.Select(g => g.Item1.IconName).DistinctUntilChanged().Subscribe(iconName =>
			{
				if (iconName == null) return;
				store.Dispatch(new AddOrUpdateNamedIconsAction() { Icons = new Dictionary<string, Pixbuf>() { { iconName, iconTheme.LoadIcon(iconName, 64) } } });
			});
		});
	}

	private void LoadPanels(Display display)
	{
		new GLibSynchronizationContext().Post(_ =>
		{
			var monitors = display.GetMonitors();
			var removedPanels = _panels.Where(p => monitors.All(m => !p.IsOnMonitor(m))).ToList();
			var newMonitors = monitors.Where(m => _panels.All(p => !p.IsOnMonitor(m))).ToList();
			var remainingPanels = _panels.Except(removedPanels).ToList();
			_panels = remainingPanels;

			remainingPanels.ForEach(p =>
			{
				p.DockToBottom();
			});

			removedPanels.ForEach(w =>
			{
				w.Close();
				w.Dispose();
			});

			newMonitors.ForEach(m =>
			{
				var newPanel = serviceProvider.Resolve<Panel>(new TypedParameter(typeof(Monitor), m));
				application.AddWindow(newPanel);
				newPanel.DockToBottom();
				_panels.Add(newPanel);
			});
		}, null);
	}

	public void Shutdown()
	{
		Application.Quit();
	}
}
