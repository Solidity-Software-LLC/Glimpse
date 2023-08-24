using System.Reactive.Linq;
using Fluxor;
using GLib;
using Gtk;
using GtkNetPanel.Services;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.SystemTray;

public class SystemTrayBox : Box
{
	public SystemTrayBox(IState<SystemTrayState> trayState, IDispatcher dispatcher) : base(Orientation.Horizontal, 3)
	{
		trayState.ToObservable().ObserveOn(new GLibSynchronizationContext()).Select(x => x.Items).DistinctUntilChanged().UnbundleMany(i => i.Key).Subscribe(obs =>
		{
			var itemObservable = obs.Select(s => s.Value).DistinctUntilChanged();
			var systemTrayIcon = new SystemTrayIcon(itemObservable);
			PackStart(systemTrayIcon, false, false, 3);
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
