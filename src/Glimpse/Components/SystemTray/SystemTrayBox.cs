using System.Reactive.Linq;
using Gdk;
using GLib;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Extensions.Redux;
using Glimpse.Extensions.Redux.Selectors;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Glimpse.State.SystemTray;
using Gtk;

namespace Glimpse.Components.SystemTray;

public class SystemTrayBox : Box
{
	public SystemTrayBox(ReduxStore store, FreeDesktopService freeDesktopService) : base(Orientation.Horizontal, 0)
	{
		StyleContext.AddClass("system-tray__taskbar-container");

		var volumeButton = new Button()
			.AddClass("system-tray__icon")
			.AddMany(new Image(Assets.Volume.ScaleSimple(24, 24, InterpType.Bilinear)));

		volumeButton.ObserveEvent<ButtonReleaseEventArgs>(nameof(ButtonReleaseEvent))
			.WithLatestFrom(store.Select(RootStateSelectors.VolumeCommand))
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
