using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Fluxor;
using Fluxor.Selectors;
using GLib;
using Glimpse.Components.Shared.ForEach;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;

namespace Glimpse.Components.Taskbar;

public class TaskbarView : Box
{
	public TaskbarView(IStore store, IDisplayServer displayServer, FreeDesktopService freeDesktopService, IDispatcher dispatcher)
	{
		var viewModelSelector = store
			.SubscribeSelector(TaskbarSelectors.ViewModel)
			.ToObservable()
			.TakeUntilDestroyed(this)
			.ObserveOn(new GLibSynchronizationContext())
			.Replay(1);

		var forEachGroup = ForEachExtensions.Create(viewModelSelector.Select(g => g.Groups).DistinctUntilChanged(), i => i.Id, viewModelObservable =>
		{
			var replayLatestViewModelObservable = viewModelObservable.Replay(1);
			var contextMenu = new TaskbarGroupContextMenu(viewModelObservable);
			var windowPicker = new TaskbarWindowPicker(viewModelObservable);
			var groupIcon = new TaskbarGroupIcon(viewModelObservable, windowPicker);

			viewModelObservable
				.Select(v => v.DesktopFile.IniFile.FilePath)
				.DistinctUntilChanged()
				.Subscribe(p => groupIcon.Data[ForEachDataKeys.Uri] = "file:///" + p);

			Observable.FromEventPattern(windowPicker, nameof(windowPicker.VisibilityNotifyEvent))
				.Subscribe(_ => windowPicker.CenterAbove(groupIcon));

			windowPicker.PreviewWindowClicked
				.Subscribe(windowId =>
				{
					windowPicker.ClosePopup();
					displayServer.MakeWindowVisible(windowId);
				});

			windowPicker.CloseWindow
				.WithLatestFrom(viewModelObservable.Select(vm => vm.Tasks.Count).DistinctUntilChanged())
				.Where(t => t.Second == 1)
				.Subscribe(_ => windowPicker.ClosePopup());

			windowPicker.CloseWindow
				.Subscribe(displayServer.CloseWindow);

			var cancelOpen = groupIcon.ObserveEvent(nameof(LeaveNotifyEvent))
				.Merge(groupIcon.ObserveEvent(nameof(Unmapped)));

			groupIcon.ObserveEvent<EnterNotifyEventArgs>(nameof(EnterNotifyEvent))
				.WithLatestFrom(replayLatestViewModelObservable)
				.Where(t => t.Second.Tasks.Count > 0)
				.Select(t => Observable.Timer(TimeSpan.FromMilliseconds(250), new SynchronizationContextScheduler(new GLibSynchronizationContext())).TakeUntil(cancelOpen).Select(_ => t.Second))
				.Switch()
				.Where(_ => !windowPicker.Visible)
				.Subscribe(t =>
				{
					dispatcher.Dispatch(new TakeScreenshotAction() { Windows = t.Tasks.Select(x => x.WindowRef).ToList() });
					windowPicker.Popup();
				});

			var cancelClose = groupIcon.ObserveEvent(nameof(EnterNotifyEvent))
				.Merge(windowPicker.ObserveEvent(nameof(EnterNotifyEvent)));

			groupIcon.ObserveEvent(nameof(LeaveNotifyEvent)).Merge(windowPicker.ObserveEvent(nameof(LeaveNotifyEvent)))
				.Select(_ => Observable.Timer(TimeSpan.FromMilliseconds(250), new SynchronizationContextScheduler(new GLibSynchronizationContext())).TakeUntil(cancelClose))
				.Switch()
				.Where(_ => !windowPicker.IsPointerInside())
				.Subscribe(_ => windowPicker.ClosePopup());

			groupIcon.ContextMenuOpened
				.Subscribe(_ => contextMenu.Popup());

			groupIcon.ObserveEvent<ButtonPressEventArgs>(nameof(groupIcon.ButtonPressEvent))
				.Subscribe(_ => windowPicker.ClosePopup());

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
		forEachGroup.OrderingChanged.Subscribe(t => dispatcher.Dispatch(new UpdateGroupOrderingAction() { GroupId = t.Item1, NewIndex = t.Item2 }));
		forEachGroup.DragBeginObservable.Subscribe(icon => icon.CloseWindowPicker());

		Add(forEachGroup);

		viewModelSelector.Connect();
	}
}
