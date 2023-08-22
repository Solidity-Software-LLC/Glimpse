using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GLib;
using Gtk;
using GtkNetPanel.Components.ApplicationBar.Components;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarView : Box
{
	public ApplicationBarView(ApplicationBarController controller)
	{
		var forEachObs = controller.ViewModel.Select(g => g.Groups).DistinctUntilChanged().UnbundleMany(g => g.ApplicationName);
		var forEachGroup = new ForEach<ApplicationBarGroupViewModel>(forEachObs, viewModelObservable =>
		{
			var replayLatestViewModelObservable = viewModelObservable.Replay(1);
			var contextMenu = new ApplicationGroupContextMenu(viewModelObservable);
			var windowPicker = new WindowPicker(viewModelObservable);
			var groupIcon = new ApplicationGroupIcon(viewModelObservable);

			Observable.FromEventPattern(windowPicker, nameof(windowPicker.VisibilityNotifyEvent))
				.Subscribe(_ => windowPicker.CenterAbove(groupIcon));

			windowPicker.PreviewWindowClicked
				.Subscribe(w => controller.MakeWindowVisible(w));

			windowPicker.CloseWindow
				.Subscribe(w => controller.CloseWindow(w));

			Observable.FromEventPattern<EnterNotifyEventArgs>(groupIcon, nameof(EnterNotifyEvent))
				.WithLatestFrom(replayLatestViewModelObservable)
				.Where(t => t.Second.Tasks.Count > 0)
				.Delay(TimeSpan.FromMilliseconds(250), new SynchronizationContextScheduler(new GLibSynchronizationContext()))
				.TakeUntil(Observable.FromEventPattern<LeaveNotifyEventArgs>(groupIcon, nameof(LeaveNotifyEvent)))
				.Repeat()
				.Where(_ => !windowPicker.Visible)
				.Subscribe(_ => windowPicker.Popup(), e => Console.WriteLine(e), () => { });

			groupIcon.ContextMenuOpened
				.Subscribe(_ => contextMenu.Popup());

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count == 0)
				.Subscribe(t => controller.Launch(t.Second));

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count == 1)
				.Subscribe(t => controller.ToggleWindowVisibility(t.Second.Tasks.First().WindowRef));

			groupIcon.ButtonRelease
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.Button == 1 && t.Second.Tasks.Count > 1 && !windowPicker.Visible)
				.Subscribe(_ => windowPicker.Popup());

			contextMenu.WindowAction
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => controller.HandleWindowAction(t.First, t.Second));

			contextMenu.DesktopFileAction
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => controller.HandleDesktopFileAction(t.First, t.Second));

			contextMenu.Pin
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => controller.TogglePinning(t.Second));

			contextMenu.Launch
				.WithLatestFrom(viewModelObservable)
				.Subscribe(t => controller.Launch(t.Second));

			replayLatestViewModelObservable.Connect();
			return groupIcon;
		});

		forEachGroup.Orientation = Orientation.Horizontal;
		Add(forEachGroup);
	}
}
