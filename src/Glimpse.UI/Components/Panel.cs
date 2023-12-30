using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Autofac.Features.AttributeFilters;
using Gdk;
using GLib;
using Glimpse.Common.Gtk;
using Glimpse.Configuration;
using Glimpse.Freedesktop.DesktopEntries;
using Glimpse.Redux;
using Glimpse.UI.Components.Calendar;
using Glimpse.UI.Components.Shared;
using Glimpse.UI.Components.StartMenu;
using Glimpse.UI.Components.SystemTray;
using Glimpse.UI.Components.Taskbar;
using Glimpse.UI.State;
using Gtk;
using ReactiveMarbles.ObservableEvents;
using DateTime = System.DateTime;
using Menu = Gtk.Menu;
using Monitor = Gdk.Monitor;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.UI.Components;

public class Panel : Window
{
	private readonly Monitor _monitor;
	private readonly Menu _menu;
	private const string ClockFormat = "h:mm tt\nM/d/yyyy";

	public Panel(
		SystemTrayBox systemTrayBox,
		TaskbarView taskbarView,
		StartMenuLaunchIcon startMenuLaunchIcon,
		ReduxStore store,
		FreeDesktopService freeDesktopService,
		Monitor monitor,
		[KeyFilter(Timers.OneSecond)] IObservable<DateTime> oneSecondTimer,
		CalendarWindow calendarWindow) : base(WindowType.Toplevel)
	{
		_monitor = monitor;
		Decorated = false;
		Resizable = false;
		TypeHint = WindowTypeHint.Dock;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		this.Events().DeleteEvent.Subscribe(e => e.RetVal = true);
		this.ObserveButtonRelease().Subscribe(_ => Window.Focus(0));

		var centerBox = new Box(Orientation.Horizontal, 0);
		centerBox.PackStart(startMenuLaunchIcon, false, false, 0);
		centerBox.PackStart(taskbarView, false, false, 0);
		centerBox.Halign = Align.Start;

		var clock = CreateClock();
		var clockLabel = clock.Image as Label;
		clock.ObserveButtonRelease().Where(e => e.Event.Button == 1).Subscribe(e =>
		{
			calendarWindow.ToggleVisibility();
			e.RetVal = true;
		});

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

		store.Select(TaskbarSelectors.Slots)
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Select(g => g.Refs.Count)
			.DistinctUntilChanged()
			.Subscribe(numGroups => { centerBox.MarginStart = ComputeCenterBoxMarginLeft(numGroups); });

		oneSecondTimer
			.TakeUntilDestroyed(this)
			.ObserveOn(new GLibSynchronizationContext())
			.Select(dt => dt.ToString(ClockFormat))
			.DistinctUntilChanged()
			.Subscribe(t => clockLabel.Text = t);

		var taskManagerObs = store
			.Select(ConfigurationSelectors.TaskManagerCommand)
			.TakeUntilDestroyed(this)
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));

		var taskManagerMenuItem = ContextMenuHelper.CreateMenuItem("Task Manager", new ImageViewModel() { Image = Assets.TaskManager.Scale(ThemeConstants.MenuItemIconSize) });
		taskManagerMenuItem.ObserveButtonRelease().WithLatestFrom(taskManagerObs).Subscribe(t => freeDesktopService.Run(t.Second));

		_menu = new Menu();
		_menu.ReserveToggleSize = false;
		_menu.Add(taskManagerMenuItem);
		_menu.ShowAll();

		this.CreateContextMenuObservable().Subscribe(t => _menu.Popup());
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

	private Button CreateClock()
	{
		var clock = new Label(DateTime.Now.ToString(ClockFormat));
		clock.Justify = Justification.Right;

		var clockButton = new Button();
		clockButton.AddClass("clock");
		clockButton.AddButtonStates();
		clockButton.Image = clock;
		return clockButton;
	}

	public void DockToBottom()
	{
		var monitorDimensions = _monitor.Geometry;
		SetSizeRequest(monitorDimensions.Width, AllocatedHeight);
		Move(_monitor.Workarea.Left, _monitor.Geometry.Bottom - AllocatedHeight + 1);
		ReserveSpace();
	}

	private void ReserveSpace()
	{
		var reservedSpaceLong = new long[] { 0, 0, 0, AllocatedHeight, 0, 0, 0, 0, 0, 0, _monitor.Workarea.Left, _monitor.Workarea.Left + _monitor.Geometry.Width - 1 }.SelectMany(BitConverter.GetBytes).ToArray();
		Property.Change(Window, Atom.Intern("_NET_WM_STRUT_PARTIAL", false), Atom.Intern("CARDINAL", false), 32, PropMode.Replace, reservedSpaceLong, 12);
	}

	public bool IsOnMonitor(Monitor monitor)
	{
		return monitor.Handle == _monitor.Handle;
	}
}