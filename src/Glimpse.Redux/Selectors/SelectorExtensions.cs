using System.Collections.Immutable;

namespace Glimpse.Redux.Selectors;

public static class SelectorExtensions
{
	public static ISelector<TResult> WithComparer<TResult>(this ISelector<TResult> selector, Func<TResult, TResult, bool> areEqual)
	{
		return SelectorFactory.CreateSelector(selector, s => s, areEqual);
	}

	public static ISelector<ImmutableList<T>> WithSequenceComparer<T>(this ISelector<ImmutableList<T>> selector, Func<T, T, bool> f)
	{
		return SelectorFactory.CreateSelector(selector, s => s, FuncEqualityComparer<ImmutableList<T>>.Create((x, y) =>
		{
			return x.SequenceEqual(y, FuncEqualityComparer<T>.Create(f));
		}));
	}
}
