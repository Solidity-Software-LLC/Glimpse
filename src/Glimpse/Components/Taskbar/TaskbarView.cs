using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GLib;
using Glimpse.Components.Shared.ForEach;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Redux;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;

namespace Glimpse.Components.Taskbar;

public class TaskbarView : Box
{
	public TaskbarView(ReduxStore store, IDisplayServer displayServer, FreeDesktopService freeDesktopService)
	{
		var viewModelSelector = store
			.Select(TaskbarSelectors.ViewModel)
			.TakeUntilDestroyed(this)
			.ObserveOn(new GLibSynchronizationContext())
			.Replay(1);

		var forEachGroup = ForEachExtensions.Create(viewModelSelector.Select(g => g.Groups).DistinctUntilChanged(), i => i.SlotRef, viewModelObservable =>
		{
			var replayLatestViewModelObservable = viewModelObservable.Replay(1);
			var contextMenu = new TaskbarGroupContextMenu(viewModelObservable.Select(vm => vm.ContextMenu).DistinctUntilChanged());
			var windowPicker = new TaskbarWindowPicker(viewModelObservable);
			var groupIcon = new TaskbarGroupIcon(viewModelObservable, windowPicker);

			windowPicker.ObserveEvent(nameof(windowPicker.VisibilityNotifyEvent))
				.Subscribe(_ => windowPicker.CenterAbove(groupIcon));

			windowPicker.PreviewWindowClicked
				.TakeUntilDestroyed(this)
				.Subscribe(windowId =>
				{
					windowPicker.ClosePopup();
					displayServer.MakeWindowVisible(windowId);
				});

			windowPicker.CloseWindow
				.TakeUntilDestroyed(this)
				.WithLatestFrom(viewModelObservable.Select(vm => vm.Tasks.Count).DistinctUntilChanged())
				.Where(t => t.Second == 1)
				.Subscribe(_ => windowPicker.ClosePopup());

			windowPicker.CloseWindow
				.TakeUntilDestroyed(this)
				.Subscribe(displayServer.CloseWindow);

			var cancelOpen = groupIcon.ObserveEvent(nameof(LeaveNotifyEvent))
				.Merge(groupIcon.ObserveEvent(nameof(Unmapped)))
				.Merge(Observable.FromEventPattern(this, nameof(Destroyed)))
				.Take(1);

			groupIcon.ObserveEvent<EnterNotifyEventArgs>(nameof(EnterNotifyEvent))
				.WithLatestFrom(replayLatestViewModelObservable)
				.Where(t => t.Second.Tasks.Count > 0)
				.Select(t => Observable.Timer(TimeSpan.FromMilliseconds(400), new SynchronizationContextScheduler(new GLibSynchronizationContext())).TakeUntil(cancelOpen).Select(_ => t.Second))
				.Switch()
				.Where(_ => !windowPicker.Visible)
				.Subscribe(t =>
				{
					store.Dispatch(new TakeScreenshotAction() { Windows = t.Tasks.Select(x => x.WindowRef).ToList() });
					windowPicker.Popup();
				});

			var cancelClose = groupIcon.ObserveEvent(nameof(EnterNotifyEvent))
				.Merge(windowPicker.ObserveEvent(nameof(EnterNotifyEvent)));

			groupIcon.ObserveEvent(nameof(LeaveNotifyEvent)).Merge(windowPicker.ObserveEvent(nameof(LeaveNotifyEvent)))
				.Select(_ => Observable.Timer(TimeSpan.FromMilliseconds(400), new SynchronizationContextScheduler(new GLibSynchronizationContext())).TakeUntil(cancelClose))
				.Switch()
				.Where(_ => !windowPicker.IsPointerInside())
				.Subscribe(_ => windowPicker.ClosePopup());

			groupIcon.CreateContextMenuObservable()
				.Subscribe(_ => contextMenu.Popup());

			groupIcon.ObserveEvent<ButtonPressEventArgs>(nameof(groupIcon.ButtonPressEvent))
				.Subscribe(_ => windowPicker.ClosePopup());

			groupIcon.ObserveButtonRelease()
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Event.Button == 1 && t.Second.Tasks.Count == 0)
				.Subscribe(t => freeDesktopService.Run(t.Second.DesktopFile));

			groupIcon.ObserveButtonRelease()
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Event.Button == 1 && t.Second.Tasks.Count == 1)
				.Subscribe(t => displayServer.ToggleWindowVisibility(t.Second.Tasks.First().WindowRef));

			groupIcon.ObserveButtonRelease()
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Event.Button == 1 && t.Second.Tasks.Count > 1 && !windowPicker.Visible)
				.Subscribe(t =>
				{
					store.Dispatch(new TakeScreenshotAction() { Windows = t.Second.Tasks.Select(x => x.WindowRef).ToList() });
					windowPicker.Popup();
				});

			contextMenu.WindowAction
				.TakeUntilDestroyed(this)
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => t.Second.Tasks.ForEach(task => displayServer.CloseWindow(task.WindowRef)));

			contextMenu.DesktopFileAction
				.TakeUntilDestroyed(this)
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => freeDesktopService.Run(t.First));

			contextMenu.Pin
				.TakeUntilDestroyed(this)
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => store.Dispatch(new ToggleTaskbarPinningAction(t.Second.DesktopFile.Id)));

			contextMenu.Launch
				.TakeUntilDestroyed(this)
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => freeDesktopService.Run(t.Second.DesktopFile));

			replayLatestViewModelObservable.Connect();
			return groupIcon;
		});

		forEachGroup.ColumnSpacing = 0;
		forEachGroup.MaxChildrenPerLine = 100;
		forEachGroup.MinChildrenPerLine = 100;
		forEachGroup.RowSpacing = 0;
		forEachGroup.Orientation = Orientation.Horizontal;
		forEachGroup.Homogeneous = false;
		forEachGroup.Valign = Align.Center;
		forEachGroup.Halign = Align.Start;
		forEachGroup.SelectionMode = SelectionMode.None;
		forEachGroup.Expand = false;
		forEachGroup.AddClass("taskbar__container");
		forEachGroup.OrderingChanged
			.TakeUntilDestroyed(this)
			.Subscribe(t => store.Dispatch(new UpdateTaskbarSlotOrderingSingleAction() { SlotRef = t.Item1.SlotRef, NewIndex = t.Item2 }));
		forEachGroup.DragBeginObservable.TakeUntilDestroyed(this).Subscribe(icon => icon.CloseWindowPicker());

		Add(forEachGroup);

		viewModelSelector.Connect();
	}
}
