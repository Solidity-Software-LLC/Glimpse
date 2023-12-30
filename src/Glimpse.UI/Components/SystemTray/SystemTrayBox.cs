using System.Reactive.Linq;
using GLib;
using Glimpse.Common.System.Reactive;
using Glimpse.Configuration;
using Glimpse.Freedesktop;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Redux;
using Gtk;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.UI.Components.SystemTray;

public class SystemTrayBox : Box
{
	public SystemTrayBox(ReduxStore store, FreeDesktopService freeDesktopService) : base(Orientation.Horizontal, 0)
	{
		StyleContext.AddClass("system-tray__taskbar-container");

		var volumeIcon = new Image();
		volumeIcon.SetFromIconName("audio-volume-medium", IconSize.Dialog);
		volumeIcon.PixelSize = 24;

		var volumeButton = new Button()
			.AddClass("system-tray__icon")
			.AddMany(volumeIcon);

		volumeButton.ObserveEvent(w => w.Events().ButtonReleaseEvent)
			.WithLatestFrom(store.Select(ConfigurationSelectors.VolumeCommand))
			.Subscribe(t => freeDesktopService.Run(t.Second));

		PackEnd(volumeButton, false, false, 0);

		store
			.Select(SystemTraySelectors.ViewModel)
			.TakeUntilDestroyed(this)
			.ObserveOn(new GLibSynchronizationContext())
			.Select(x => x.Items)
			.DistinctUntilChanged()
			.UnbundleMany(i => i.Id)
			.RemoveIndex()
			.Subscribe(itemObservable =>
			{
				var systemTrayIcon = new SystemTrayIcon(itemObservable);
				PackStart(systemTrayIcon, false, false, 0);
				ShowAll();

				systemTrayIcon.MenuItemActivated.TakeUntilDestroyed(this).WithLatestFrom(itemObservable).Subscribe(t =>
				{
					store.Dispatch(new ActivateMenuItemAction() { DbusObjectDescription = t.Second.DbusMenuDescription, MenuItemId = t.First });
				});

				systemTrayIcon.ApplicationActivated.TakeUntilDestroyed(this).WithLatestFrom(itemObservable).Subscribe(t =>
				{
					store.Dispatch(new ActivateApplicationAction() { DbusObjectDescription = t.Second.StatusNotifierItemDescription, X = t.First.Item1, Y = t.First.Item2 });
				});

				itemObservable.TakeLast(1).Subscribe(_ => systemTrayIcon.Destroy());
			});
	}
}
