using Fluxor;
using GtkNetPanel.Interop.X11;
using GtkNetPanel.Services.FreeDesktop;
using GtkNetPanel.Services.X11;
using GtkNetPanel.State;

namespace GtkNetPanel.Services.DisplayServer;

public class X11DisplayServer : IDisplayServer
{
	private readonly XLibAdaptorService _xService;
	private readonly IDispatcher _dispatcher;
	private readonly FreeDesktopService _freeDesktopService;

	public X11DisplayServer(XLibAdaptorService xService, IDispatcher dispatcher, FreeDesktopService freeDesktopService)
	{
		_xService = xService;
		_dispatcher = dispatcher;
		_freeDesktopService = freeDesktopService;
	}

	public void ToggleWindowVisibility(GenericWindowRef windowRef)
	{
		_xService.ToggleWindowVisibility((XWindowRef)windowRef.InternalRef);
	}

	public void MakeWindowVisible(GenericWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef)windowRef.InternalRef);
	}

	public void CloseWindow(TaskState taskState)
	{
		_xService.CloseWindow((XWindowRef)taskState.WindowRef.InternalRef);
	}

	public void MaximizeWindow(GenericWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef.InternalRef);
		_xService.MaximizeWindow((XWindowRef) windowRef.InternalRef);
	}

	public void MinimizeWindow(GenericWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef.InternalRef);
		_xService.MinimizeWindow((XWindowRef) windowRef.InternalRef);
	}

	public void StartResizing(GenericWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef.InternalRef);
		_xService.StartResizing((XWindowRef) windowRef.InternalRef);
	}

	public void StartMoving(GenericWindowRef windowRef)
	{
		_xService.MakeWindowVisible((XWindowRef) windowRef.InternalRef);
		_xService.StartMoving((XWindowRef) windowRef.InternalRef);
	}

	public void Initialize()
	{
		_xService.WindowCreated.Subscribe(w =>
		{
			_dispatcher.Dispatch(new AddTaskAction() { Task = CreateTask(w) });
		});

		_xService.WindowRemoved.Subscribe(w =>
		{
			_dispatcher.Dispatch(new RemoveTaskAction() { WindowId = $"{w.Display}_{w.Window}"});
		});

		_xService.FocusChanged.Subscribe(w =>
		{
			_dispatcher.Dispatch(new UpdateFocusAction() { WindowRef = new GenericWindowRef() { Id = $"{w.Display}_{w.Window}", InternalRef = w } });
		});
	}

	private TaskState CreateTask(XWindowRef windowRef)
	{
		var allowedX11WindowActions = _xService.GetAtomArray(windowRef, XAtoms.NetWmAllowedActions);
		var windowActions = ParseWindowActions(allowedX11WindowActions);
		var desktopFile = _freeDesktopService.FindAppDesktopFile(_xService.GetClassHint(windowRef).res_name);

		return new TaskState()
		{
			Title = _xService.GetStringProperty(windowRef, XAtoms.NetWmName),
			WindowRef = new GenericWindowRef() { Id = $"{windowRef.Display}_{windowRef.Window}", InternalRef = windowRef },
			Icons = _xService.GetIcons(windowRef),
			State = _xService.GetAtomArray(windowRef, XAtoms.NetWmState).ToList(),
			ApplicationName = desktopFile.Name,
			DesktopFile = desktopFile,
			AllowedActions = windowActions,
			Screenshot = _xService.CaptureWindowScreenshot(windowRef)
		};
	}

	private AllowedWindowActions[] ParseWindowActions(string[] x11WindowActions)
	{
		var results = new LinkedList<AllowedWindowActions>();

		foreach (var a in x11WindowActions)
		{
			var words = a.ToLower().Split("_", StringSplitOptions.RemoveEmptyEntries);
			var wordsProperCased = words.Select(s => char.ToUpper(s[0]) + s[1..]);
			var normalizedName = string.Join("", wordsProperCased.Skip(3));

			if (normalizedName.Contains("Maximize"))
			{
				results.AddLast(AllowedWindowActions.Maximize);
			}
			else if (Enum.TryParse(normalizedName, out AllowedWindowActions enumValue))
			{
				results.AddLast(enumValue);
			}
		}

		results.AddLast(AllowedWindowActions.Maximize);
		results.AddLast(AllowedWindowActions.Minimize);
		return results.Distinct().ToArray();
	}
}
