using System.Reactive.Linq;

namespace Glimpse.Extensions.Redux.Selectors;

public sealed class MemoizedSelector<TSelectorResult, TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	private readonly Cache<TSelectorResult> _cachedSelectorResult = new();

	public MemoizedSelector(ISelector<TSelectorResult> selector, Func<TSelectorResult, TOutput> projectorFunction)
	{
		Selector = selector;
		ProjectorFunction = projectorFunction;
	}

	public ISelector<TSelectorResult> Selector { get; }
	public Func<TSelectorResult, TOutput> ProjectorFunction { get; }

	public TOutput Apply(StoreState input)
	{
		var selectorResult = Selector.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult.ValueEquals(selectorResult))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult.Value = selectorResult;
		_cachedOutput.Value = ProjectorFunction(selectorResult);
		return _cachedOutput.Value;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Apply).DistinctUntilChanged();
	}
}

public sealed class MemoizedSelector<TSelectorResult1, TSelectorResult2, TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	private readonly Cache<TSelectorResult1> _cachedSelectorResult1 = new();
	private readonly Cache<TSelectorResult2> _cachedSelectorResult2 = new();

	public MemoizedSelector(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, Func<TSelectorResult1, TSelectorResult2, TOutput> projectorFunction)
	{
		Selector1 = selector1;
		Selector2 = selector2;
		ProjectorFunction = projectorFunction;
	}

	public ISelector<TSelectorResult1> Selector1 { get; }
	public ISelector<TSelectorResult2> Selector2 { get; }
	public Func<TSelectorResult1, TSelectorResult2, TOutput> ProjectorFunction { get; }

	public TOutput Apply(StoreState input)
	{
		var selector1Result = Selector1.Apply(input);
		var selector2Result = Selector2.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;

		_cachedOutput.Value = ProjectorFunction(selector1Result, selector2Result);

		return _cachedOutput.Value;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Apply).DistinctUntilChanged();
	}
}

public sealed class MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TOutput> : ISelector<TOutput>
{
	private readonly Cache<TSelectorResult1> _cachedSelectorResult1 = new();
	private readonly Cache<TSelectorResult2> _cachedSelectorResult2 = new();
	private readonly Cache<TSelectorResult3> _cachedSelectorResult3 = new();
	private TOutput _cachedOutput;

	public MemoizedSelector(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TOutput> projectorFunction)
	{
		Selector1 = selector1;
		Selector2 = selector2;
		Selector3 = selector3;
		ProjectorFunction = projectorFunction;
	}

	public ISelector<TSelectorResult1> Selector1 { get; }
	public ISelector<TSelectorResult2> Selector2 { get; }
	public ISelector<TSelectorResult3> Selector3 { get; }
	public Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TOutput> ProjectorFunction { get; }

	public TOutput Apply(StoreState input)
	{
		var selector1Result = Selector1.Apply(input);
		var selector2Result = Selector2.Apply(input);
		var selector3Result = Selector3.Apply(input);

		if (_cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result))
		{
			return _cachedOutput;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;

		_cachedOutput = ProjectorFunction(selector1Result, selector2Result, selector3Result);

		return _cachedOutput;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Apply).DistinctUntilChanged();
	}
}

public sealed class MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TOutput> : ISelector<TOutput>
{
	private readonly Cache<TSelectorResult1> _cachedSelectorResult1 = new();
	private readonly Cache<TSelectorResult2> _cachedSelectorResult2 = new();
	private readonly Cache<TSelectorResult3> _cachedSelectorResult3 = new();
	private readonly Cache<TSelectorResult4> _cachedSelectorResult4 = new();
	private TOutput _cachedOutput;

	public MemoizedSelector(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TOutput> projectorFunction)
	{
		Selector1 = selector1;
		Selector2 = selector2;
		Selector3 = selector3;
		Selector4 = selector4;
		ProjectorFunction = projectorFunction;
	}

	public ISelector<TSelectorResult1> Selector1 { get; }
	public ISelector<TSelectorResult2> Selector2 { get; }
	public ISelector<TSelectorResult3> Selector3 { get; }
	public ISelector<TSelectorResult4> Selector4 { get; }

	public Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TOutput> ProjectorFunction
	{
		get;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = Selector1.Apply(input);
		var selector2Result = Selector2.Apply(input);
		var selector3Result = Selector3.Apply(input);
		var selector4Result = Selector4.Apply(input);

		if (_cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result))
		{
			return _cachedOutput;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;

		_cachedOutput = ProjectorFunction(selector1Result, selector2Result, selector3Result, selector4Result);

		return _cachedOutput;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Apply).DistinctUntilChanged();
	}
}

public sealed class MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TOutput> : ISelector<TOutput>
{
	private readonly Cache<TSelectorResult1> _cachedSelectorResult1 = new();
	private readonly Cache<TSelectorResult2> _cachedSelectorResult2 = new();
	private readonly Cache<TSelectorResult3> _cachedSelectorResult3 = new();
	private readonly Cache<TSelectorResult4> _cachedSelectorResult4 = new();
	private readonly Cache<TSelectorResult5> _cachedSelectorResult5 = new();
	private TOutput _cachedOutput;

	public MemoizedSelector(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, ISelector<TSelectorResult5> selector5, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TOutput> projectorFunction)
	{
		Selector1 = selector1;
		Selector2 = selector2;
		Selector3 = selector3;
		Selector4 = selector4;
		Selector5 = selector5;
		ProjectorFunction = projectorFunction;
	}

	public ISelector<TSelectorResult1> Selector1 { get; }
	public ISelector<TSelectorResult2> Selector2 { get; }
	public ISelector<TSelectorResult3> Selector3 { get; }
	public ISelector<TSelectorResult4> Selector4 { get; }
	public ISelector<TSelectorResult5> Selector5 { get; }
	public Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TOutput> ProjectorFunction { get; }

	public TOutput Apply(StoreState input)
	{
		var selector1Result = Selector1.Apply(input);
		var selector2Result = Selector2.Apply(input);
		var selector3Result = Selector3.Apply(input);
		var selector4Result = Selector4.Apply(input);
		var selector5Result = Selector5.Apply(input);

		if (_cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result) && _cachedSelectorResult5.ValueEquals(selector5Result))
		{
			return _cachedOutput;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;
		_cachedSelectorResult5.Value = selector5Result;

		_cachedOutput = ProjectorFunction(selector1Result, selector2Result, selector3Result, selector4Result, selector5Result);

		return _cachedOutput;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Apply).DistinctUntilChanged();
	}
}

public sealed class MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TOutput> : ISelector<TOutput>
{
	private readonly Cache<TSelectorResult1> _cachedSelectorResult1 = new();
	private readonly Cache<TSelectorResult2> _cachedSelectorResult2 = new();
	private readonly Cache<TSelectorResult3> _cachedSelectorResult3 = new();
	private readonly Cache<TSelectorResult4> _cachedSelectorResult4 = new();
	private readonly Cache<TSelectorResult5> _cachedSelectorResult5 = new();
	private readonly Cache<TSelectorResult6> _cachedSelectorResult6 = new();
	private TOutput _cachedOutput;

	public MemoizedSelector(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, ISelector<TSelectorResult5> selector5, ISelector<TSelectorResult6> selector6, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TOutput> projectorFunction)
	{
		Selector1 = selector1;
		Selector2 = selector2;
		Selector3 = selector3;
		Selector4 = selector4;
		Selector5 = selector5;
		Selector6 = selector6;
		ProjectorFunction = projectorFunction;
	}

	public ISelector<TSelectorResult1> Selector1 { get; }
	public ISelector<TSelectorResult2> Selector2 { get; }
	public ISelector<TSelectorResult3> Selector3 { get; }
	public ISelector<TSelectorResult4> Selector4 { get; }
	public ISelector<TSelectorResult5> Selector5 { get; }
	public ISelector<TSelectorResult6> Selector6 { get; }
	public Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TOutput> ProjectorFunction { get; }

	public TOutput Apply(StoreState input)
	{
		var selector1Result = Selector1.Apply(input);
		var selector2Result = Selector2.Apply(input);
		var selector3Result = Selector3.Apply(input);
		var selector4Result = Selector4.Apply(input);
		var selector5Result = Selector5.Apply(input);
		var selector6Result = Selector6.Apply(input);

		if (_cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result) && _cachedSelectorResult5.ValueEquals(selector5Result) && _cachedSelectorResult6.ValueEquals(selector6Result))
		{
			return _cachedOutput;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;
		_cachedSelectorResult5.Value = selector5Result;
		_cachedSelectorResult6.Value = selector6Result;

		_cachedOutput = ProjectorFunction(selector1Result, selector2Result, selector3Result, selector4Result, selector5Result, selector6Result);

		return _cachedOutput;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Apply).DistinctUntilChanged();
	}
}

public sealed class MemoizedSelector<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TSelectorResult7, TOutput> : ISelector<TOutput>
{
	private readonly Cache<TSelectorResult1> _cachedSelectorResult1 = new();
	private readonly Cache<TSelectorResult2> _cachedSelectorResult2 = new();
	private readonly Cache<TSelectorResult3> _cachedSelectorResult3 = new();
	private readonly Cache<TSelectorResult4> _cachedSelectorResult4 = new();
	private readonly Cache<TSelectorResult5> _cachedSelectorResult5 = new();
	private readonly Cache<TSelectorResult6> _cachedSelectorResult6 = new();
	private readonly Cache<TSelectorResult7> _cachedSelectorResult7 = new();
	private TOutput _cachedOutput;

	public MemoizedSelector(ISelector<TSelectorResult1> selector1, ISelector<TSelectorResult2> selector2, ISelector<TSelectorResult3> selector3, ISelector<TSelectorResult4> selector4, ISelector<TSelectorResult5> selector5, ISelector<TSelectorResult6> selector6, ISelector<TSelectorResult7> selector7, Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TSelectorResult7, TOutput> projectorFunction)
	{
		Selector1 = selector1;
		Selector2 = selector2;
		Selector3 = selector3;
		Selector4 = selector4;
		Selector5 = selector5;
		Selector6 = selector6;
		Selector7 = selector7;
		ProjectorFunction = projectorFunction;
	}

	public ISelector<TSelectorResult1> Selector1 { get; }
	public ISelector<TSelectorResult2> Selector2 { get; }
	public ISelector<TSelectorResult3> Selector3 { get; }
	public ISelector<TSelectorResult4> Selector4 { get; }
	public ISelector<TSelectorResult5> Selector5 { get; }
	public ISelector<TSelectorResult6> Selector6 { get; }
	public ISelector<TSelectorResult7> Selector7 { get; }
	public Func<TSelectorResult1, TSelectorResult2, TSelectorResult3, TSelectorResult4, TSelectorResult5, TSelectorResult6, TSelectorResult7, TOutput> ProjectorFunction { get; }

	public TOutput Apply(StoreState input)
	{
		var selector1Result = Selector1.Apply(input);
		var selector2Result = Selector2.Apply(input);
		var selector3Result = Selector3.Apply(input);
		var selector4Result = Selector4.Apply(input);
		var selector5Result = Selector5.Apply(input);
		var selector6Result = Selector6.Apply(input);
		var selector7Result = Selector7.Apply(input);

		if (_cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result) && _cachedSelectorResult5.ValueEquals(selector5Result) && _cachedSelectorResult6.ValueEquals(selector6Result) && _cachedSelectorResult7.ValueEquals(selector7Result))
		{
			return _cachedOutput;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;
		_cachedSelectorResult5.Value = selector5Result;
		_cachedSelectorResult6.Value = selector6Result;
		_cachedSelectorResult7.Value = selector7Result;

		_cachedOutput = ProjectorFunction(selector1Result, selector2Result, selector3Result, selector4Result, selector5Result, selector6Result, selector7Result);

		return _cachedOutput;
	}

	public IObservable<TOutput> Apply(IObservable<StoreState> input)
	{
		return input.Select(Apply).DistinctUntilChanged();
	}
}
