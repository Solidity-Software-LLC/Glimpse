using System.Collections.Immutable;

namespace Glimpse.Extensions.Redux.Selectors;

public static class SelectorExtensions
{
	public static ISelector<TResult> WithComparer<TResult>(this ISelector<TResult> selector, Func<TResult, TResult, bool> areEqual)
	{
		return SelectorFactory.CreateSelector(selector, s => s, areEqual);
	}

	public static ISelector<ImmutableList<T>> WithSequenceComparer<T>(this ISelector<ImmutableList<T>> selector, Func<T, T, bool> f)
	{
		return SelectorFactory.CreateSelector(selector, s => s, new FuncEqualityComparer<ImmutableList<T>>((x, y) =>
		{
			return x.SequenceEqual(y, new FuncEqualityComparer<T>(f));
		}));
	}
}
