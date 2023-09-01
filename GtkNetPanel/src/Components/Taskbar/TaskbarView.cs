using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Fluxor;
using GLib;
using Gtk;
using GtkNetPanel.Extensions.Gtk;
using GtkNetPanel.Extensions.Reactive;
using GtkNetPanel.Services;
using GtkNetPanel.Services.DisplayServer;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.Taskbar;

public class TaskbarView : Box
{
	public TaskbarView(TaskbarSelectors selectors, IDisplayServer displayServer, FreeDesktopService freeDesktopService, IDispatcher dispatcher)
	{
		var viewModelSelector = selectors.ViewModel
			.TakeUntilDestroyed(this)
			.ObserveOn(new GLibSynchronizationContext());

		var forEachObs = viewModelSelector.Select(g => g.Groups).DistinctUntilChanged().UnbundleMany(g => g.ApplicationName);
		var forEachGroup = new ForEach<ApplicationBarGroupViewModel>(forEachObs, viewModelObservable =>
		{
			var replayLatestViewModelObservable = viewModelObservable.Replay(1);
			var contextMenu = new TaskbarGroupContextMenu(viewModelObservable);
			var windowPicker = new TaskbarWindowPicker(viewModelObservable);
			var groupIcon = new TaskbarGroupIcon(viewModelObservable);

			Observable.FromEventPattern(windowPicker, nameof(windowPicker.VisibilityNotifyEvent))
				.Subscribe(_ => windowPicker.CenterAbove(groupIcon));

			windowPicker.PreviewWindowClicked
				.Subscribe(displayServer.MakeWindowVisible);

			windowPicker.CloseWindow
				.Subscribe(displayServer.CloseWindow);

			Observable.FromEventPattern<EnterNotifyEventArgs>(groupIcon, nameof(EnterNotifyEvent))
				.WithLatestFrom(replayLatestViewModelObservable)
				.Where(t => t.Second.Tasks.Count > 0)
				.Delay(TimeSpan.FromMilliseconds(250), new SynchronizationContextScheduler(new GLibSynchronizationContext()))
				.TakeUntil(Observable.FromEventPattern<LeaveNotifyEventArgs>(groupIcon, nameof(LeaveNotifyEvent)))
				.Repeat()
				.Where(_ => !windowPicker.Visible)
				.Subscribe(_ => windowPicker.Popup(), Console.WriteLine, () => { });

			groupIcon.ContextMenuOpened
				.Subscribe(_ => contextMenu.Popup());

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count == 0)
				.Subscribe(t => freeDesktopService.Run(t.Second.DesktopFile.Exec.FullExec));

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count == 1)
				.Subscribe(t => displayServer.ToggleWindowVisibility(t.Second.Tasks.First().WindowRef));

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count > 1 && !windowPicker.Visible)
				.Subscribe(_ => windowPicker.Popup());

			contextMenu.WindowAction
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => t.Second.Tasks.ForEach(task => displayServer.CloseWindow(task.WindowRef)));

			contextMenu.DesktopFileAction
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => freeDesktopService.Run(t.First));

			contextMenu.Pin
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => dispatcher.Dispatch(new ToggleTaskbarPinningAction() { DesktopFile = t.Second.DesktopFile }));

			contextMenu.Launch
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => freeDesktopService.Run(t.Second.DesktopFile.Exec.FullExec));

			replayLatestViewModelObservable.Connect();
			return groupIcon;
		});

		forEachGroup.Orientation = Orientation.Horizontal;
		Add(forEachGroup);
	}
}
