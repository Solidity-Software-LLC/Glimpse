using Glimpse.Extensions.Redux.Effects;
using Glimpse.Services.DisplayServer;
using static Glimpse.Extensions.Redux.Effects.EffectsFactory;

namespace Glimpse.State;

public class Effects(IDisplayServer displayServer) : IEffectsFactory
{
	public IEnumerable<Effect> Create() => new[]
	{
		CreateEffect<TakeScreenshotAction>(action => new[]
		{
			new UpdateScreenshotsAction()
			{
				Screenshots = action.Windows
					.Select(w => (w.Id, displayServer.TakeScreenshot(w))).Where(t => t.Item2 != null)
					.ToDictionary(t => t.Id, t => t.Item2)
			}
		}),
	};
}
