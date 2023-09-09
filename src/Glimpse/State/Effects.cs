using Fluxor;
using Glimpse.Services.DisplayServer;

namespace Glimpse.State;

public class Effects
{
	private readonly IDisplayServer _displayServer;

	public Effects(IDisplayServer displayServer)
	{
		_displayServer = displayServer;
	}

	[EffectMethod]
	public Task HandleTakeScreenshotAction(TakeScreenshotAction action, IDispatcher dispatcher)
	{
		var screenshots = new LinkedList<(IWindowRef Window, BitmapImage Screenshot)>();

		foreach (var w in action.Windows)
		{
			var screenshot = _displayServer.TakeScreenshot(w);
			if (screenshot != null) screenshots.AddLast((w, screenshot));
		}

		dispatcher.Dispatch(new UpdateScreenshotsAction() { Screenshots = screenshots });
		return Task.CompletedTask;
	}
}
