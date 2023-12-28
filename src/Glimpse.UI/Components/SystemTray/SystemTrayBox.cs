using System.Reactive.Linq;
using GLib;
using Glimpse.Configuration;
using Glimpse.Freedesktop;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Lib.Gtk;
using Glimpse.Lib.System.Reactive;
using Glimpse.Redux;
using Glimpse.Redux.Selectors;
using Gtk;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.UI.Components.SystemTray;

public class SystemTrayBox : Box
{
	public SystemTrayBox(ReduxStore store, FreeDesktopService freeDesktopService) : base(Orientation.Horizontal, 0)
	{
		StyleContext.AddClass("system-tray__taskbar-container");

		var volumeButton = new Button()
			.AddClass("system-tray__icon")
			.AddMany(new Image(Assets.Volume.Scale(24).ToPixbuf()));

		volumeButton.ObserveEvent(w => w.Events().ButtonReleaseEvent)
			.WithLatestFrom(store.Select(ConfigurationSelectors.VolumeCommand))
			.Subscribe(t => freeDesktopService.Run(t.Second));

		PackEnd(volumeButton, false, false, 0);

		store.Select(SelectorFactory.CreateSelector(s => s.GetFeatureState<SystemTrayState>())).TakeUntilDestroyed(this).ObserveOn(new GLibSynchronizationContext()).Select(x => x.Items).DistinctUntilChanged().UnbundleMany(i => i.Key).RemoveIndex().Subscribe(obs =>
		{
			var itemObservable = obs.TakeUntilDestroyed(this).Select(s => s.Value).DistinctUntilChanged();
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

			itemObservable.Subscribe(_ => { }, _ => { }, () => systemTrayIcon.Destroy());
		});
	}
}
