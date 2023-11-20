namespace Glimpse.Extensions.Redux.Selectors;

public interface ISelector<out TOutput>
{
	TOutput Apply(StoreState input);
	IObservable<TOutput> Apply(IObservable<StoreState> input);
}
