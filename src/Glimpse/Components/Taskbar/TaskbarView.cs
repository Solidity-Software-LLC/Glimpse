using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Cairo;
using Fluxor;
using Gdk;
using GLib;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.DisplayServer;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;
using Drag = Gtk.Drag;

namespace Glimpse.Components.Taskbar;

public class TaskbarView : Box
{
	private int? _dragXPosition;
	private ForEach<TaskbarGroupViewModel> _forEachGroup;

	public TaskbarView(TaskbarSelectors selectors, IDisplayServer displayServer, FreeDesktopService freeDesktopService, IDispatcher dispatcher)
	{
		var viewModelSelector = selectors.ViewModel
			.TakeUntilDestroyed(this)
			.ObserveOn(new GLibSynchronizationContext());

		Drag.DestSet(this, 0, null, 0);

		var forEachObs = viewModelSelector.Select(g => g.Groups).DistinctUntilChanged().UnbundleMany(g => g.Id);
		_forEachGroup = new ForEach<TaskbarGroupViewModel>(forEachObs, viewModelObservable =>
		{
			var replayLatestViewModelObservable = viewModelObservable.Replay(1);
			var contextMenu = new TaskbarGroupContextMenu(viewModelObservable);
			var windowPicker = new TaskbarWindowPicker(viewModelObservable);
			var groupIcon = new TaskbarGroupIcon(viewModelObservable);

			Drag.SourceSet(groupIcon, ModifierType.Button1Mask, null, DragAction.Move);

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

		Add(_forEachGroup);
	}

	protected override void OnDragLeave(DragContext context, uint time)
	{
		_dragXPosition = null;
	}

	protected override void OnDragEnd(DragContext context)
	{
		_dragXPosition = null;
		QueueDraw();
	}

	protected override bool OnDragMotion(DragContext context, int x, int y, uint time)
	{
		_dragXPosition = x;
		QueueDraw();
		Gdk.Drag.Status(context, context.SuggestedAction, time);
		return true;
	}

	protected override bool OnDragDrop(DragContext context, int x, int y, uint time)
	{
		var sourceWidget = Drag.GetSourceWidget(context);
		var oldIndex = _forEachGroup.Children.ToList().IndexOf(sourceWidget);
		var newIndex = FindIndexOfDrop(x);
		if (newIndex > oldIndex) newIndex--;
		_forEachGroup.ReorderChild(sourceWidget, newIndex);
		_dragXPosition = null;
		QueueDraw();
		return base.OnDragDrop(context, x, y, time);
	}

	private int FindIndexOfDrop(int x)
	{
		var childContainer = (Container) Children[0];
		var childWidget = childContainer.FindChildAtX(x);
		var firstChild = childContainer.Children.First();
		var lastChild = childContainer.Children.Last();

		firstChild.TranslateCoordinates(this, 0, 0, out var firstChildX, out _);
		lastChild.TranslateCoordinates(this, 0, 0, out var lastChildX, out _);

		if (x <= firstChildX) childWidget = firstChild;
		else if (x >= lastChildX + lastChild.Allocation.Width) childWidget = lastChild;

		childWidget.TranslateCoordinates(this, 0, 0, out var left, out _);
		var childIndex = childContainer.Children.ToList().FindIndex(w => w == childWidget);

		var iconWidth = childWidget.Allocation.Width;
		var center = left + iconWidth / 2;
		return x > center ? childIndex + 1 : childIndex;
	}

	protected override bool OnDrawn(Context cr)
	{
		if (_dragXPosition.HasValue)
		{
			var childContainer = (Container) Children[0];
			var childWidget = childContainer.FindChildAtX(_dragXPosition.Value);
			var firstChild = childContainer.Children.First();
			var lastChild = childContainer.Children.Last();

			firstChild.TranslateCoordinates(this, 0, 0, out var firstChildX, out _);
			lastChild.TranslateCoordinates(this, 0, 0, out var lastChildX, out _);

			if (_dragXPosition.Value <= firstChildX) childWidget = firstChild;
			else if (_dragXPosition.Value >= lastChildX + lastChild.Allocation.Width) childWidget = lastChild;

			childWidget.TranslateCoordinates(this, 0, 0, out var left, out _);
			var iconWidth = childWidget.Allocation.Width;
			var right = left + iconWidth;
			var center = left + iconWidth / 2;
			var lineXPosition = _dragXPosition > center ? right + 2 : left - 2;

			var lineHeight = childWidget.HeightRequest + 4;
			var marginTop = (Allocation.Height - lineHeight) / 2;

			cr.MoveTo(lineXPosition, marginTop);
			cr.LineTo(lineXPosition, Allocation.Height - marginTop);
			cr.SetSourceRGBA(1, 1, 1, 0.6);
			cr.Stroke();
		}

		return base.OnDrawn(cr);
	}
}
