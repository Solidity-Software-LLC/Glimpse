namespace Glimpse.Extensions.Redux.Selectors;

public static class SelectorFactory
{
	public static ISelector<TFinalResult> CreateFeatureSelector<TFinalResult>() where TFinalResult : class
	{
		return new SimpleSelector<TFinalResult>(s => s.GetFeatureState<TFinalResult>());
	}

	public static ISelector<TFinalResult> CreateSelector<TFinalResult>(Func<StoreState, TFinalResult> projectorFunction)
	{
		return new SimpleSelector<TFinalResult>(projectorFunction);
	}

	public static ISelector<TFinalResult> CreateSelector<TSelectorResult1, TFinalResult>(ISelector<TSelectorResult1> selector1, Func<TSelectorResult1, TFinalResult> projectorFunction)
	{
		return new MemoizedSelector<TSelectorResult1, TFinalResult>(selector1, projectorFunction);
	}

	public static ISelector<TFinalResult> CreateSelector<TSelectorResult1, TSelectorResult2, TFinalResult>(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, Func<TSelectorResult1, TSelectorResult2, TFinalResult> projectorFunction)
	{
		return new MemoizedSelector<TSelectorResult1, TSelectorResult2, TFinalResult>(selector1, selector2, projectorFunction);
	}

	public static ISelector<TFinalResult> CreateSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TFinalResult>(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TFinalResult> projectorFunction)
	{
		return new MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TFinalResult>(selector1, selector2, selector3, projectorFunction);
	}

	public static ISelector<TFinalResult> CreateSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TFinalResult>(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TFinalResult> projectorFunction)
	{
		return new MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TFinalResult>(selector1, selector2, selector3, selector4, projectorFunction);
	}

	public static ISelector<TFinalResult> CreateSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TFinalResult>(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, ISelector<TSelectorResult5> selector5, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TFinalResult> projectorFunction)
	{
		return new MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TFinalResult>(selector1, selector2, selector3, selector4, selector5, projectorFunction);
	}

	public static ISelector<TFinalResult> CreateSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TFinalResult>(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, ISelector<TSelectorResult5> selector5, ISelector<TSelectorResult6> selector6, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TFinalResult> projectorFunction)
	{
		return new MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TFinalResult>(selector1, selector2, selector3, selector4, selector5, selector6, projectorFunction);
	}

	public static ISelector<TFinalResult> CreateSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TSelectorResult7, TFinalResult>(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, ISelector<TSelectorResult5> selector5, ISelector<TSelectorResult6> selector6, ISelector<TSelectorResult7> selector7, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TSelectorResult7, TFinalResult> projectorFunction)
	{
		return new MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TSelectorResult7, TFinalResult>(selector1, selector2, selector3, selector4, selector5, selector6, selector7, projectorFunction);
	}
}
