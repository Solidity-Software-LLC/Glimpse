using System.Reactive.Linq;

namespace Glimpse.Extensions.Redux.Selectors;

public sealed class SimpleSelector<TOutput> : ISelector<TOutput>
{
	public SimpleSelector(Func<StoreState, TOutput> selector)
	{
		Selector = selector;
	}

	public Func<StoreState, TOutput> Selector { get; }

	public TOutput Apply(StoreState input)
	{
		return Selector(input);
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Selector).DistinctUntilChanged();
	}
}
