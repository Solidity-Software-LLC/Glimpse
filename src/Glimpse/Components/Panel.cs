using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Fluxor;
using Fluxor.Selectors;
using Gdk;
using GLib;
using Glimpse.Components.StartMenu;
using Glimpse.Components.SystemTray;
using Glimpse.Components.Taskbar;
using Glimpse.Extensions.Fluxor;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.FreeDesktop;
using Glimpse.State;
using Gtk;
using DateTime = System.DateTime;
using Monitor = Gdk.Monitor;
using Task = System.Threading.Tasks.Task;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace Glimpse.Components;

public class Panel : Window
{
	private const int PanelHeight = 52;
	private const string ClockFormat = "h:mm tt\ndddd\nM/d/yyyy";

	public Panel(SystemTrayBox systemTrayBox, TaskbarView taskbarView, StartMenuLaunchIcon startMenuLaunchIcon, IStore store, FreeDesktopService freeDesktopService) : base(WindowType.Toplevel)
	{
		Decorated = false;
		Resizable = false;
		TypeHint = WindowTypeHint.Dock;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		ButtonReleaseEvent += (_, _) => Window.Focus(0);

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

		store.SubscribeSelector(RootStateSelectors.Groups).ToObservable()
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false))
			.Select(g => g.Count)
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.Subscribe(numGroups =>
			{
				centerBox.MarginStart = ComputeCenterBoxMarginLeft(numGroups);
			});

		StartClockAsync(clock);

		var taskManagerObs = store
			.SubscribeSelector(RootStateSelectors.TaskManagerCommand)
			.ToObservable()
			.ObserveOn(new SynchronizationContextScheduler(new GLibSynchronizationContext(), false));

		var taskManagerMenuItem = ContextMenuHelper.CreateMenuItem("Task Manager", Observable.Return(Assets.TaskManager.ScaleSimple(16, 16, InterpType.Bilinear)));
		taskManagerMenuItem.ObserveButtonRelease().WithLatestFrom(taskManagerObs).Subscribe(t => freeDesktopService.Run(t.Second));

		var menu = new Gtk.Menu();
		menu.ReserveToggleSize = false;
		menu.Add(taskManagerMenuItem);
		menu.ShowAll();

		this.CreateContextMenuObservable().Subscribe(t => menu.Popup());
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

	private async Task StartClockAsync(Label clock)
	{
		await Task.Yield();

		var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

		while (await timer.WaitForNextTickAsync())
		{
			var newText = DateTime.Now.ToString(ClockFormat);

			if (clock.Text != newText)
			{
				clock.Text = newText;
				clock.QueueDraw();
			}
		}
	}

	public void DockToBottom(Monitor monitor)
	{
		var monitorDimensions = monitor.Geometry;
		SetSizeRequest(monitorDimensions.Width, PanelHeight);
		Move(monitor.Workarea.Left, monitorDimensions.Height - PanelHeight);
		ReserveSpace();
	}

	private void ReserveSpace()
	{
		var reservedSpaceLong = new long[] { 0, 0, 0, PanelHeight, 0, 0, 0, 0, 0, 0, 0, Window.Display.DefaultScreen.RootWindow.Width }.SelectMany(BitConverter.GetBytes).ToArray();
		Property.Change(Window, Atom.Intern("_NET_WM_STRUT_PARTIAL", false), Atom.Intern("CARDINAL", false), 32, PropMode.Replace, reservedSpaceLong, 12);
	}
}
