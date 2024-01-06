using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Text;
using Autofac;
using Gdk;
using GLib;
using Glimpse.Common.System.Reactive;
using Glimpse.Freedesktop.Notifications;
using Glimpse.Redux;
using Glimpse.UI.Components;
using Glimpse.UI.Components.Calendar;
using Glimpse.UI.Components.Notifications;
using Glimpse.UI.Components.StartMenu.Window;
using Glimpse.UI.State;
using Gtk;
using Microsoft.Extensions.Hosting;
using ReactiveMarbles.ObservableEvents;
using Application = Gtk.Application;
using Monitor = Gdk.Monitor;
using Task = System.Threading.Tasks.Task;

namespace Glimpse.UI;

public class GlimpseGtkApplication(ILifetimeScope serviceProvider, Application application, ReduxStore store) : IHostedService
{
	private List<Panel> _panels = new();

	public Task StartAsync(CancellationToken cancellationToken)
	{
		Task.Run(StartInternal);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		Application.Quit();
		return Task.CompletedTask;
	}

	private void StartInternal()
	{
		ExceptionManager.UnhandledException += args =>
		{
			Console.WriteLine("Unhandled Exception:");
			Console.WriteLine(args.ExceptionObject);
			args.ExitApplication = false;
		};

		Application.Init();
		application.Register(Cancellable.Current);
		LoadCss();
		WatchNotifications();

		var openStartMenuAction = (SimpleAction) application.LookupAction("OpenStartMenu");
		openStartMenuAction.Events().Activated.Select(_ => true).Subscribe(_ => store.Dispatch(new StartMenuOpenedAction()));

		var display = Display.Default;
		var screen = display.DefaultScreen;
		var action = (SimpleAction) application.LookupAction("LoadPanels");
		action.Events().Activated.Subscribe(_ => LoadPanels(display));
		screen.Events().SizeChanged.Subscribe(_ => LoadPanels(display));
		screen.Events().MonitorsChanged.Subscribe(_ => LoadPanels(display));
		application.AddWindow(serviceProvider.Resolve<StartMenuWindow>());
		application.AddWindow(serviceProvider.Resolve<CalendarWindow>());
		LoadPanels(display);
		Application.Run();
	}

	private void LoadCss()
	{
		var assembly = typeof(GlimpseGtkApplication).Assembly;
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

	private void StackNotificationsOnMonitor(Monitor monitor, int panelHeight, ImmutableList<NotificationWindow> notificationWindows)
	{
		var currentTopOfWidgetBelowNotification = monitor.Geometry.Height - panelHeight;

		for (var i = 0; i < notificationWindows.Count; i++)
		{
			var window = notificationWindows[i];
			var windowLeft = monitor.Workarea.Right - window.Allocation.Width - 8;
			var windowTop = currentTopOfWidgetBelowNotification - window.Allocation.Height - 8;
			window.Move(windowLeft, windowTop);
			currentTopOfWidgetBelowNotification = windowTop;
		}

	}
	private void WatchNotifications()
	{
		var notificationsPerMonitor = new Dictionary<Monitor, ImmutableList<NotificationWindow>>();
		var notificationsService = serviceProvider.Resolve<NotificationsService>();

		store
			.Select(NotificationUISelectors.ViewModel)
			.ObserveOn(new GLibSynchronizationContext())
			.Select(vm => vm.Notifications)
			.UnbundleMany(n => n.Id)
			.Subscribe(notificationObservable =>
			{
				try
				{
					var display = Display.Default;
					display.GetPointer(out var x, out var y);
					var eventMonitor = display.GetMonitorAtPoint(x, y);
					var newWindow = new NotificationWindow(notificationObservable.Select(x => x.Item1));
					application.AddWindow(newWindow);

					var panel = _panels.FirstOrDefault(p => p.IsOnMonitor(eventMonitor));
					panel.Window.GetGeometry(out _, out _, out _, out var panelHeight);

					newWindow.CloseNotification
						.TakeUntil(notificationObservable.TakeLast(1))
						.Subscribe(_ => notificationsService.DismissNotification(notificationObservable.Key));

					newWindow.ActionInvoked
						.TakeUntil(notificationObservable.TakeLast(1))
						.Subscribe(action => notificationsService.ActionInvoked(notificationObservable.Key, action));

					newWindow.Events().SizeAllocated.Take(1).TakeUntilDestroyed(newWindow).Subscribe(_ =>
					{
						var windowLeft = eventMonitor.Workarea.Right - newWindow.Allocation.Width - 8;
						var windowTop = eventMonitor.Geometry.Height - panelHeight - newWindow.Allocation.Height - 8;
						newWindow.Move(windowLeft, windowTop);
						if (notificationsPerMonitor.TryGetValue(eventMonitor, out var notificationWindows))
						{
							StackNotificationsOnMonitor(eventMonitor, panelHeight, notificationWindows);
						}
					});

					notificationsPerMonitor.TryAdd(eventMonitor, ImmutableList<NotificationWindow>.Empty);
					notificationsPerMonitor[eventMonitor] = notificationsPerMonitor[eventMonitor].Add(newWindow);

					notificationObservable.TakeLast(1).ObserveOn(new GLibSynchronizationContext()).Subscribe(_ =>
					{
						newWindow.Dispose();
						notificationsPerMonitor[eventMonitor] = notificationsPerMonitor[eventMonitor].Remove(newWindow);

						if (notificationsPerMonitor.TryGetValue(eventMonitor, out var notificationWindows))
						{
							StackNotificationsOnMonitor(eventMonitor, panelHeight, notificationWindows);
						}
					});
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
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
}
