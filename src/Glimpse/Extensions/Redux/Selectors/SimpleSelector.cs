using System.Reactive.Linq;

namespace Glimpse.Extensions.Redux.Selectors;

public sealed class SimpleSelector<TOutput> : ISelector<TOutput>
{
	private readonly IEqualityComparer<TOutput> _equalityComparer;
	private readonly Cache<TOutput> _cachedOutput = new();

	public SimpleSelector(Func<StoreState, TOutput> selector, IEqualityComparer<TOutput> equalityComparer = null)
	{
		_equalityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		Selector = selector;
	}

	public Func<StoreState, TOutput> Selector { get; }

	public TOutput Apply(StoreState input)
	{
		var result = Selector(input);

		if (!_equalityComparer.Equals(_cachedOutput.Value, result))
		{
			_cachedOutput.Value = result;
		}

		return _cachedOutput.Value;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Selector).DistinctUntilChanged();
	}
}
