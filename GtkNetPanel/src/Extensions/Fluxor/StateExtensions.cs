using System.Reactive.Linq;
using Fluxor;

namespace GtkNetPanel.Extensions.Fluxor;

public static class StateExtensions
{
	public static IObservable<T> ToObservable<T>(this IState<T> state)
	{
		return Observable.Create<T>(o =>
		{
			o.OnNext(state.Value);
			return Observable.FromEventPattern(state, nameof(state.StateChanged)).Select(e => state.Value).DistinctUntilChanged().Subscribe(o);
		});
	}
}
