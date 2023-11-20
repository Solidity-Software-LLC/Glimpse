using System.Reactive.Concurrency;
using System.Reactive.Linq;
using GLib;
using Glimpse.Components.StartMenu.Window;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Redux;
using Glimpse.Services.FreeDesktop;
using Gtk;
using Menu = Gtk.Menu;
using MenuItem = Gtk.MenuItem;

namespace Glimpse.Components.StartMenu;

public class StartMenuLaunchIcon : EventBox
{
	public StartMenuLaunchIcon(FreeDesktopService freeDesktopService, ReduxStore store, StartMenuWindow startMenuWindow)
	{
		var viewModelObservable = store.Select(StartMenuSelectors.ViewModel)
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Replay(1);

		startMenuWindow.ObserveEvent(nameof(startMenuWindow.Shown)).TakeUntilDestroyed(this)
			.Merge(startMenuWindow.ObserveEvent(nameof(startMenuWindow.Hidden)).TakeUntilDestroyed(this))
			.Merge(startMenuWindow.WindowMoved.TakeUntilDestroyed(this).Select(x => (object) x))
			.Subscribe(_ =>
			{
				if (!startMenuWindow.IsVisible || Display.GetMonitorAtWindow(Window) != Display.GetMonitorAtWindow(startMenuWindow.Window))
				{
					StyleContext.RemoveClass("start-menu__launch-icon--open");
				}
				else
				{
					StyleContext.AddClass("start-menu__launch-icon--open");
				}
			});

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		this.AddClass("start-menu__launch-icon");

		var image = new Image();
		image.SetSizeRequest(42, 42);
		Add(image);

		var iconObservable = Observable.Return((Assets.MenuIcon.Scale(38), Assets.MenuIcon.Scale(32))).Replay(1);
		this.AppIcon(image, iconObservable);
		this.ObserveEvent<ButtonReleaseEventArgs>(nameof(ButtonReleaseEvent)).Where(e => e.Event.Button == 1).Subscribe(e =>
		{
			startMenuWindow.ToggleVisibility();
			e.RetVal = true;
		});

		var launchIconMenu = new Menu();

		viewModelObservable.Select(vm => vm.LaunchIconContextMenu).DistinctUntilChanged().Subscribe(menuItems =>
		{
			launchIconMenu.RemoveAllChildren();

			foreach (var i in menuItems)
			{
				if (i.DisplayText.Equals("separator", StringComparison.OrdinalIgnoreCase))
				{
					launchIconMenu.Add(new SeparatorMenuItem());
				}
				else
				{
					var menuItem = new MenuItem(i.DisplayText);
					menuItem.ObserveEvent(nameof(menuItem.Activated)).Subscribe(_ => freeDesktopService.Run(i.Executable + " " + i.Arguments));
					launchIconMenu.Add(menuItem);
				}
			}

			launchIconMenu.ShowAll();
		});

		this.CreateContextMenuObservable().Subscribe(_ => launchIconMenu.Popup());

		viewModelObservable.Connect();
		iconObservable.Connect();
	}
}
