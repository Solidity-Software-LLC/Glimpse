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
		dispatcher.Dispatch(new UpdateScreenshotsAction()
		{
			Screenshots = action.Windows
				.Select(w => (w.Id, _displayServer.TakeScreenshot(w))).Where(t => t.Item2 != null)
				.ToDictionary(t => t.Id, t => t.Item2)
		});

		return Task.CompletedTask;
	}
}
