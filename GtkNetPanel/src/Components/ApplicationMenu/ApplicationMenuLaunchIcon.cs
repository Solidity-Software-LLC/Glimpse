using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxor;
using Gdk;
using GLib;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuLaunchIcon : EventBox
{
	private readonly Subject<EventButton> _buttonRelease = new();

	public ApplicationMenuLaunchIcon(IState<RootState> stateObservable, FreeDesktopService freeDesktopService)
	{
		var pinnedAppsObservable = stateObservable
			.ToObservable()
			.Select(s => s.AppMenuPinnedDesktopFiles)
			.DistinctUntilChanged();

		var allAppsObservable = stateObservable
			.ToObservable()
			.Select(s => s.DesktopFiles)
			.DistinctUntilChanged();

		var viewModelObservable = pinnedAppsObservable
			.CombineLatest(allAppsObservable)
			.Select(t => new ApplicationMenuViewModel() { PinnedFiles = t.First, DesktopFiles = t.Second })
			.ObserveOn(new GLibSynchronizationContext());

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		StyleContext.AddClass("app-menu__launch-icon");
		SetSizeRequest(42, 42);
		this.AddHoverHighlighting();

		var imageBuffer = Assets.Ubuntu;
		imageBuffer = imageBuffer.ScaleSimple(28, 28, InterpType.Bilinear);
		Add(new Image(imageBuffer));

		var appMenuWindow = new ApplicationMenuWindow(viewModelObservable);

		appMenuWindow.AppLaunch.Subscribe(f =>
		{
			appMenuWindow.Hide();
			freeDesktopService.Run(f.Exec.FullExec);
		});

		_buttonRelease
			.Where(_ => !appMenuWindow.Visible)
			.Subscribe(_ => appMenuWindow.Popup(), e => Console.WriteLine(e), () => { });

		Observable.FromEventPattern(appMenuWindow, nameof(appMenuWindow.VisibilityNotifyEvent))
			.Subscribe(_ => appMenuWindow.CenterOnScreenAboveWidget(this));
	}

	protected override bool OnButtonReleaseEvent(EventButton evnt)
	{
		_buttonRelease.OnNext(evnt);
		return true;
	}
}
