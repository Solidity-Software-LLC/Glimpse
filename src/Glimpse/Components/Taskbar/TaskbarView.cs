using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Fluxor;
using Gdk;
using GLib;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;

namespace Glimpse.Components.Taskbar;

public class TaskbarView : Box
{
	public TaskbarView(TaskbarSelectors selectors, IDisplayServer displayServer, FreeDesktopService freeDesktopService, IDispatcher dispatcher)
	{
		var viewModelSelector = selectors.ViewModel
			.TakeUntilDestroyed(this)
			.ObserveOn(new GLibSynchronizationContext());

		var forEachGroup = new ForEach<TaskbarGroupViewModel, string, TaskbarGroupIcon>(viewModelSelector.Select(g => g.Groups).DistinctUntilChanged(), i => i.Id, (viewModelObservable, _) =>
		{
			var replayLatestViewModelObservable = viewModelObservable.Replay(1);
			var contextMenu = new TaskbarGroupContextMenu(viewModelObservable);
			var windowPicker = new TaskbarWindowPicker(viewModelObservable);
			var groupIcon = new TaskbarGroupIcon(viewModelObservable, windowPicker);

			Observable.FromEventPattern(windowPicker, nameof(windowPicker.VisibilityNotifyEvent))
				.Subscribe(_ => windowPicker.CenterAbove(groupIcon));

			windowPicker.PreviewWindowClicked
				.Subscribe(displayServer.MakeWindowVisible);

			windowPicker.CloseWindow
				.Subscribe(displayServer.CloseWindow);

			groupIcon.ObserveEvent<EnterNotifyEventArgs>(nameof(EnterNotifyEvent))
				.WithLatestFrom(replayLatestViewModelObservable)
				.Where(t => t.Second.Tasks.Count > 0)
				.Delay(TimeSpan.FromMilliseconds(250), new SynchronizationContextScheduler(new GLibSynchronizationContext()))
				.TakeUntil(groupIcon.ObserveEvent(nameof(LeaveNotifyEvent)).Merge(groupIcon.ObserveEvent(nameof(Unmapped))))
				.Repeat()
				.Where(_ => !windowPicker.Visible)
				.Subscribe(t =>
				{
					dispatcher.Dispatch(new TakeScreenshotAction() { Windows = t.Second.Tasks.Select(x => x.WindowRef).ToList() });
					windowPicker.Popup();
				});

			groupIcon.ContextMenuOpened
				.Subscribe(_ => contextMenu.Popup());

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count == 0)
				.Subscribe(t => freeDesktopService.Run(t.Second.DesktopFile));

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count == 1)
				.Subscribe(t => displayServer.ToggleWindowVisibility(t.Second.Tasks.First().WindowRef));

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count > 1 && !windowPicker.Visible)
				.Subscribe(t =>
				{
					dispatcher.Dispatch(new TakeScreenshotAction() { Windows = t.Second.Tasks.Select(x => x.WindowRef).ToList() });
					windowPicker.Popup();
				});

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
				.Subscribe(t => freeDesktopService.Run(t.Second.DesktopFile));

			replayLatestViewModelObservable.Connect();
			return groupIcon;
		});

		forEachGroup.DragBeginObservable.Subscribe(icon =>
		{
			icon.CloseWindowPicker();
		});

		forEachGroup.OrderingChanged.Subscribe(t =>
		{
			dispatcher.Dispatch(new UpdateGroupOrderingAction() { GroupId = t.Item1, NewIndex = t.Item2 });
		});

		Add(forEachGroup);
	}
}
