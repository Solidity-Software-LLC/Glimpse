using System.Reactive.Linq;
using Fluxor;
using Gdk;
using GLib;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Glimpse.State.SystemTray;
using Gtk;

namespace Glimpse.Components.SystemTray;

public class SystemTrayBox : Box
{
	public SystemTrayBox(IState<SystemTrayState> trayState, IDispatcher dispatcher, RootStateSelectors rootStateSelectors, FreeDesktopService freeDesktopService) : base(Orientation.Horizontal, 0)
	{
		StyleContext.AddClass("system-tray__taskbar-container");

		var volumeButton = new Button()
			.AddClass("system-tray__icon")
			.AddMany(new Image(Assets.Volume.ScaleSimple(24, 24, InterpType.Bilinear)));

		volumeButton.ObserveEvent<ButtonReleaseEventArgs>(nameof(ButtonReleaseEvent))
			.WithLatestFrom(rootStateSelectors.VolumeCommand)
			.Subscribe(t => freeDesktopService.Run(t.Second));

		PackEnd(volumeButton, false, false, 0);

		trayState.ToObservable().ObserveOn(new GLibSynchronizationContext()).Select(x => x.Items).DistinctUntilChanged().UnbundleMany(i => i.Key).Subscribe(obs =>
		{
			var itemObservable = obs.Select(s => s.Value).DistinctUntilChanged();
			var systemTrayIcon = new SystemTrayIcon(itemObservable);
			PackStart(systemTrayIcon, false, false, 0);
			ShowAll();

			systemTrayIcon.MenuItemActivated.WithLatestFrom(itemObservable).Subscribe(t =>
			{
				dispatcher.Dispatch(new ActivateMenuItemAction() { DbusObjectDescription = t.Second.DbusMenuDescription, MenuItemId = t.First });
			});

			systemTrayIcon.ApplicationActivated.WithLatestFrom(itemObservable).Subscribe(t =>
			{
				dispatcher.Dispatch(new ActivateApplicationAction() { DbusObjectDescription = t.Second.StatusNotifierItemDescription, X = t.First.Item1, Y = t.First.Item2 });
			});

			itemObservable.Subscribe(_ => { }, _ => { }, () => Remove(systemTrayIcon));
		});
	}
}
