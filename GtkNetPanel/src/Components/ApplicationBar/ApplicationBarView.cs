using System.Reactive.Linq;
using Gtk;
using GtkNetPanel.Components.ApplicationBar.Components;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarView : Box
{
	public ApplicationBarView(Application application, ApplicationBarController controller)
	{
		var forEachObs = controller.ViewModel.Select(g => g.Groups).DistinctUntilChanged().UnbundleMany(g => g.ApplicationName).DistinctUntilChanged();
		var forEachGroup = new ForEach<ApplicationBarGroupViewModel>(forEachObs, viewModelObservable =>
		{
			var contextMenu = new ApplicationGroupContextMenu(viewModelObservable);
			var windowPicker = new WindowPicker(viewModelObservable, application);
			var groupIcon = new ApplicationGroupIcon(viewModelObservable);

			Observable.FromEventPattern(windowPicker, nameof(windowPicker.VisibilityNotifyEvent))
				.Subscribe(_ => windowPicker.CenterAbove(groupIcon));

			windowPicker.PreviewWindowClicked
				.Subscribe(w => controller.MakeWindowVisible(w));

			groupIcon.ContextMenuOpened
				.Subscribe(_ => contextMenu.Popup());

			Observable.FromEventPattern<ButtonReleaseEventArgs>(groupIcon, nameof(ButtonReleaseEvent))
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.EventArgs.Event.Button == 1 && t.Second.Tasks.Count == 0)
				.Subscribe(t => controller.Launch(t.Second));

			Observable.FromEventPattern<ButtonReleaseEventArgs>(groupIcon, nameof(ButtonReleaseEvent))
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.EventArgs.Event.Button == 1 && t.Second.Tasks.Count == 1)
				.Subscribe(t => controller.ToggleWindowVisibility(t.Second.Tasks.First().WindowRef));

			Observable.FromEventPattern<ButtonReleaseEventArgs>(groupIcon, nameof(ButtonReleaseEvent))
				.WithLatestFrom(viewModelObservable)
				.Where(t => t.First.EventArgs.Event.Button == 1 && t.Second.Tasks.Count > 1)
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

			return groupIcon;
		});

		forEachGroup.Orientation = Orientation.Horizontal;
		Add(forEachGroup);
	}
}
