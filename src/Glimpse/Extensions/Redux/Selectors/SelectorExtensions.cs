namespace Glimpse.Extensions.Redux.Selectors;

public static class SelectorExtensions
{
	public static ISelector<TResult> WithComparer<TResult>(this ISelector<TResult> selector, Func<TResult, TResult, bool> areEqual)
	{
		return SelectorFactory.CreateSelector(selector, s => s, areEqual);
	}
}
