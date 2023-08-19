using System.Reactive.Linq;
using Gtk;
using GtkNetPanel.Components.ApplicationBar.Components;
using GtkNetPanel.Services;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarView : Box
{
	private readonly Application _application;
	private readonly ApplicationBarController _controller;

	public ApplicationBarView(IObservable<ApplicationBarViewModel> viewModelObservable, Application application, ApplicationBarController controller)
	{
		_application = application;
		_controller = controller;

		viewModelObservable.Select(v => v.Groups).UnbundleMany(g => g.Key).Subscribe(groupedObservable =>
		{
			var obs = groupedObservable.Select(g => g.Value).DistinctUntilChanged();
			var groupIcon = CreateApplicationGroup(obs);
			PackStart(groupIcon, false, false, 2);
			ShowAll();
			obs.Subscribe(_ => { }, _ => { }, () => Remove(groupIcon));
		});
	}

	private Widget CreateApplicationGroup(IObservable<IconGroupViewModel> viewModelObservable)
	{
		var contextMenu = new ApplicationGroupContextMenu(viewModelObservable);
		var windowPicker = new WindowPicker(viewModelObservable, _application);
		var groupIcon = new ApplicationGroupIcon(viewModelObservable);

		Observable.FromEventPattern(windowPicker, nameof(windowPicker.VisibilityNotifyEvent))
			.Subscribe(_ => windowPicker.CenterAbove(groupIcon));

		windowPicker.PreviewWindowClicked
			.Subscribe(w => _controller.MakeWindowVisible(w));

		groupIcon.ContextMenuOpened
			.Subscribe(_ => contextMenu.Popup());

		Observable.FromEventPattern<ButtonReleaseEventArgs>(groupIcon, nameof(ButtonReleaseEvent))
			.WithLatestFrom(viewModelObservable)
			.Where(t => t.First.EventArgs.Event.Button == 1 && t.Second.Tasks.Count == 1)
			.Subscribe(t => _controller.ToggleWindowVisibility(t.Second.Tasks.First().WindowRef));

		Observable.FromEventPattern<ButtonReleaseEventArgs>(groupIcon, nameof(ButtonReleaseEvent))
			.WithLatestFrom(viewModelObservable)
			.Where(t => t.First.EventArgs.Event.Button == 1 && t.Second.Tasks.Count > 1)
			.Subscribe(_ => windowPicker.Popup());

		contextMenu.WindowAction
			.WithLatestFrom(viewModelObservable)
			.Subscribe(t => _controller.HandleWindowAction(t.First, t.Second));

		return groupIcon;
	}
}
