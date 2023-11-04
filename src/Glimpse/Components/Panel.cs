using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Fluxor;
using Fluxor.Selectors;
using Gdk;
using GLib;
using Glimpse.Components.Shared;
using Glimpse.Components.StartMenu;
using Glimpse.Components.SystemTray;
using Glimpse.Components.Taskbar;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;
using DateTime = System.DateTime;
using Menu = Gtk.Menu;
using Monitor = Gdk.Monitor;
using Task = System.Threading.Tasks.Task;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.Components;

public class Panel : Window
{
	private readonly Menu _menu;
	private const string ClockFormat = "h:mm tt\ndddd\nM/d/yyyy";

	public Panel(SystemTrayBox systemTrayBox, TaskbarView taskbarView, StartMenuLaunchIcon startMenuLaunchIcon, IStore store, FreeDesktopService freeDesktopService, Monitor monitor) : base(WindowType.Toplevel)
	{
		Decorated = false;
		Resizable = false;
		TypeHint = WindowTypeHint.Dock;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		this.ObserveButtonRelease().Subscribe(_ => Window.Focus(0));

		var centerBox = new Box(Orientation.Horizontal, 0);
		centerBox.PackStart(startMenuLaunchIcon, false, false, 0);
		centerBox.PackStart(taskbarView, false, false, 0);
		centerBox.Halign = Align.Start;

		var clock = CreateClock();

		var rightBox = new Box(Orientation.Horizontal, 0);
		rightBox.PackStart(systemTrayBox, false, false, 4);
		rightBox.PackStart(clock, false, false, 5);
		rightBox.Halign = Align.End;
		rightBox.Valign = Align.Center;

		var grid = new Grid();
		grid.Attach(centerBox, 1, 0, 8, 1);
		grid.Attach(rightBox, 8, 0, 2, 1);
		grid.Vexpand = true;
		grid.Hexpand = true;
		grid.RowHomogeneous = true;
		grid.ColumnHomogeneous = true;
		grid.StyleContext.AddClass("panel");
		Add(grid);
		ShowAll();

		store.SubscribeSelector(TaskbarSelectors.Slots).ToObservable()
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Select(g => g.Refs.Count)
			.DistinctUntilChanged()
			.Subscribe(numGroups => { centerBox.MarginStart = ComputeCenterBoxMarginLeft(numGroups); });

		var clockCancellation = new CancellationTokenSource();

		Observable.FromEventPattern(this, nameof(Destroyed))
			.Take(1)
			.Subscribe(_ => clockCancellation.Cancel());

		StartClockAsync(clock, clockCancellation.Token);

		var taskManagerObs = store
			.SubscribeSelector(RootStateSelectors.TaskManagerCommand)
			.ToObservable()
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));

		var taskManagerMenuItem = ContextMenuHelper.CreateMenuItem("Task Manager", Assets.TaskManager.Scale(ThemeConstants.MenuItemIconSize));
		taskManagerMenuItem.ObserveButtonRelease().WithLatestFrom(taskManagerObs).Subscribe(t => freeDesktopService.Run(t.Second));

		_menu = new Menu();
		_menu.ReserveToggleSize = false;
		_menu.Add(taskManagerMenuItem);
		_menu.ShowAll();

		this.CreateContextMenuObservable().Subscribe(t => _menu.Popup());
		DockToBottom(monitor);
	}

	protected override void OnDestroyed()
	{
		_menu.Destroy();
		base.OnDestroyed();
	}

	private int ComputeCenterBoxMarginLeft(int numGroups)
	{
		var taskbarWidth = (numGroups + 1) * 46;
		return WidthRequest / 2 - taskbarWidth / 2;
	}

	private Label CreateClock()
	{
		var clock = new Label(DateTime.Now.ToString(ClockFormat));
		clock.Justify = Justification.Center;
		return clock;
	}

	private async Task StartClockAsync(Label clock, CancellationToken cancellationToken)
	{
		await Task.Yield();

		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

		while (await timer.WaitForNextTickAsync(cancellationToken))
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				var newText = DateTime.Now.ToString(ClockFormat);

				if (clock.Text != newText)
				{
					clock.Text = newText;
					clock.QueueDraw();
				}
			}
		}
	}

	private void DockToBottom(Monitor monitor)
	{
		var monitorDimensions = monitor.Geometry;
		SetSizeRequest(monitorDimensions.Width, AllocatedHeight);
		Move(monitor.Workarea.Left, monitorDimensions.Height - AllocatedHeight);
		ReserveSpace(monitor);
	}

	private void ReserveSpace(Monitor monitor)
	{
		var reservedSpaceLong = new long[] { 0, 0, 0, AllocatedHeight, 0, 0, 0, 0, 0, 0, monitor.Workarea.Left, monitor.Workarea.Left + monitor.Geometry.Width }.SelectMany(BitConverter.GetBytes).ToArray();
		Property.Change(Window, Atom.Intern("_NET_WM_STRUT_PARTIAL", false), Atom.Intern("CARDINAL", false), 32, PropMode.Replace, reservedSpaceLong, 12);
	}
}
