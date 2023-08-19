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
		_xService.ToggleWindowVisibility((XWindowRef)windowRef.InternalRef);
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
	}

	private TaskState CreateTask(XWindowRef windowRef)
	{
		var allowedX11WindowActions = _xService.GetAtomArray(windowRef, XAtoms.NetWmAllowedActions);
		var windowActions = ParseWindowActions(allowedX11WindowActions);

		return new TaskState()
		{
			Title = _xService.GetStringProperty(windowRef, XAtoms.NetWmName),
			WindowRef = new GenericWindowRef() { Id = $"{windowRef.Display}_{windowRef.Window}", InternalRef = windowRef },
			Icons = _xService.GetIcons(windowRef),
			State = _xService.GetAtomArray(windowRef, XAtoms.NetWmState).ToList(),
			ApplicationName = _xService.GetClassHint(windowRef).res_name,
			DesktopFile = _freeDesktopService.FindAppDesktopFile(_xService.GetClassHint(windowRef).res_name),
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

		return results.Distinct().ToArray();
	}
}
