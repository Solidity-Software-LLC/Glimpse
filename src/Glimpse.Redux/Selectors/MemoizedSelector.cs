using System.Reactive.Linq;

namespace Glimpse.Redux.Selectors;

public sealed class MemoizedSelector<S1,TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	public IEqualityComparer<TOutput> EqualityComparer { get; }
	public IObservable<TOutput> Apply(IObservable<StoreState> input) => input.Select(Apply).DistinctUntilChanged();
	public Func<Cache<TOutput>,S1,TOutput> ProjectorFunction { get; }

	private readonly Cache<S1> _cachedSelectorResult1 = new();
	private readonly ISelector<S1> _selector1;

	public MemoizedSelector(
		ISelector<S1> selector1,
		Func<Cache<TOutput>,S1, TOutput> projectorFunction,
		IEqualityComparer<TOutput> equalityComparer = null)
	{
		EqualityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		ProjectorFunction = projectorFunction;
		_selector1 = selector1;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = _selector1.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;

		var projectionResult = ProjectorFunction(_cachedOutput,selector1Result);

		if (!EqualityComparer.Equals(projectionResult, _cachedOutput.Value))
		{
			_cachedOutput.Value = projectionResult;
		}

		return _cachedOutput.Value;
	}
}
public sealed class MemoizedSelector<S1,S2,TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	public IEqualityComparer<TOutput> EqualityComparer { get; }
	public IObservable<TOutput> Apply(IObservable<StoreState> input) => input.Select(Apply).DistinctUntilChanged();
	public Func<Cache<TOutput>,S1,S2,TOutput> ProjectorFunction { get; }

	private readonly Cache<S1> _cachedSelectorResult1 = new();
	private readonly ISelector<S1> _selector1;
	private readonly Cache<S2> _cachedSelectorResult2 = new();
	private readonly ISelector<S2> _selector2;

	public MemoizedSelector(
		ISelector<S1> selector1, ISelector<S2> selector2,
		Func<Cache<TOutput>,S1,S2, TOutput> projectorFunction,
		IEqualityComparer<TOutput> equalityComparer = null)
	{
		EqualityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		ProjectorFunction = projectorFunction;
		_selector1 = selector1;
		_selector2 = selector2;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = _selector1.Apply(input);
		var selector2Result = _selector2.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;

		var projectionResult = ProjectorFunction(_cachedOutput,selector1Result,selector2Result);

		if (!EqualityComparer.Equals(projectionResult, _cachedOutput.Value))
		{
			_cachedOutput.Value = projectionResult;
		}

		return _cachedOutput.Value;
	}
}
public sealed class MemoizedSelector<S1,S2,S3,TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	public IEqualityComparer<TOutput> EqualityComparer { get; }
	public IObservable<TOutput> Apply(IObservable<StoreState> input) => input.Select(Apply).DistinctUntilChanged();
	public Func<Cache<TOutput>,S1,S2,S3,TOutput> ProjectorFunction { get; }

	private readonly Cache<S1> _cachedSelectorResult1 = new();
	private readonly ISelector<S1> _selector1;
	private readonly Cache<S2> _cachedSelectorResult2 = new();
	private readonly ISelector<S2> _selector2;
	private readonly Cache<S3> _cachedSelectorResult3 = new();
	private readonly ISelector<S3> _selector3;

	public MemoizedSelector(
		ISelector<S1> selector1, ISelector<S2> selector2, ISelector<S3> selector3,
		Func<Cache<TOutput>,S1,S2,S3, TOutput> projectorFunction,
		IEqualityComparer<TOutput> equalityComparer = null)
	{
		EqualityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		ProjectorFunction = projectorFunction;
		_selector1 = selector1;
		_selector2 = selector2;
		_selector3 = selector3;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = _selector1.Apply(input);
		var selector2Result = _selector2.Apply(input);
		var selector3Result = _selector3.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;

		var projectionResult = ProjectorFunction(_cachedOutput,selector1Result,selector2Result,selector3Result);

		if (!EqualityComparer.Equals(projectionResult, _cachedOutput.Value))
		{
			_cachedOutput.Value = projectionResult;
		}

		return _cachedOutput.Value;
	}
}
public sealed class MemoizedSelector<S1,S2,S3,S4,TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	public IEqualityComparer<TOutput> EqualityComparer { get; }
	public IObservable<TOutput> Apply(IObservable<StoreState> input) => input.Select(Apply).DistinctUntilChanged();
	public Func<Cache<TOutput>,S1,S2,S3,S4,TOutput> ProjectorFunction { get; }

	private readonly Cache<S1> _cachedSelectorResult1 = new();
	private readonly ISelector<S1> _selector1;
	private readonly Cache<S2> _cachedSelectorResult2 = new();
	private readonly ISelector<S2> _selector2;
	private readonly Cache<S3> _cachedSelectorResult3 = new();
	private readonly ISelector<S3> _selector3;
	private readonly Cache<S4> _cachedSelectorResult4 = new();
	private readonly ISelector<S4> _selector4;

	public MemoizedSelector(
		ISelector<S1> selector1, ISelector<S2> selector2, ISelector<S3> selector3, ISelector<S4> selector4,
		Func<Cache<TOutput>,S1,S2,S3,S4, TOutput> projectorFunction,
		IEqualityComparer<TOutput> equalityComparer = null)
	{
		EqualityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		ProjectorFunction = projectorFunction;
		_selector1 = selector1;
		_selector2 = selector2;
		_selector3 = selector3;
		_selector4 = selector4;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = _selector1.Apply(input);
		var selector2Result = _selector2.Apply(input);
		var selector3Result = _selector3.Apply(input);
		var selector4Result = _selector4.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;

		var projectionResult = ProjectorFunction(_cachedOutput,selector1Result,selector2Result,selector3Result,selector4Result);

		if (!EqualityComparer.Equals(projectionResult, _cachedOutput.Value))
		{
			_cachedOutput.Value = projectionResult;
		}

		return _cachedOutput.Value;
	}
}
public sealed class MemoizedSelector<S1,S2,S3,S4,S5,TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	public IEqualityComparer<TOutput> EqualityComparer { get; }
	public IObservable<TOutput> Apply(IObservable<StoreState> input) => input.Select(Apply).DistinctUntilChanged();
	public Func<Cache<TOutput>,S1,S2,S3,S4,S5,TOutput> ProjectorFunction { get; }

	private readonly Cache<S1> _cachedSelectorResult1 = new();
	private readonly ISelector<S1> _selector1;
	private readonly Cache<S2> _cachedSelectorResult2 = new();
	private readonly ISelector<S2> _selector2;
	private readonly Cache<S3> _cachedSelectorResult3 = new();
	private readonly ISelector<S3> _selector3;
	private readonly Cache<S4> _cachedSelectorResult4 = new();
	private readonly ISelector<S4> _selector4;
	private readonly Cache<S5> _cachedSelectorResult5 = new();
	private readonly ISelector<S5> _selector5;

	public MemoizedSelector(
		ISelector<S1> selector1, ISelector<S2> selector2, ISelector<S3> selector3, ISelector<S4> selector4, ISelector<S5> selector5,
		Func<Cache<TOutput>,S1,S2,S3,S4,S5, TOutput> projectorFunction,
		IEqualityComparer<TOutput> equalityComparer = null)
	{
		EqualityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		ProjectorFunction = projectorFunction;
		_selector1 = selector1;
		_selector2 = selector2;
		_selector3 = selector3;
		_selector4 = selector4;
		_selector5 = selector5;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = _selector1.Apply(input);
		var selector2Result = _selector2.Apply(input);
		var selector3Result = _selector3.Apply(input);
		var selector4Result = _selector4.Apply(input);
		var selector5Result = _selector5.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result) && _cachedSelectorResult5.ValueEquals(selector5Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;
		_cachedSelectorResult5.Value = selector5Result;

		var projectionResult = ProjectorFunction(_cachedOutput,selector1Result,selector2Result,selector3Result,selector4Result,selector5Result);

		if (!EqualityComparer.Equals(projectionResult, _cachedOutput.Value))
		{
			_cachedOutput.Value = projectionResult;
		}

		return _cachedOutput.Value;
	}
}
public sealed class MemoizedSelector<S1,S2,S3,S4,S5,S6,TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	public IEqualityComparer<TOutput> EqualityComparer { get; }
	public IObservable<TOutput> Apply(IObservable<StoreState> input) => input.Select(Apply).DistinctUntilChanged();
	public Func<Cache<TOutput>,S1,S2,S3,S4,S5,S6,TOutput> ProjectorFunction { get; }

	private readonly Cache<S1> _cachedSelectorResult1 = new();
	private readonly ISelector<S1> _selector1;
	private readonly Cache<S2> _cachedSelectorResult2 = new();
	private readonly ISelector<S2> _selector2;
	private readonly Cache<S3> _cachedSelectorResult3 = new();
	private readonly ISelector<S3> _selector3;
	private readonly Cache<S4> _cachedSelectorResult4 = new();
	private readonly ISelector<S4> _selector4;
	private readonly Cache<S5> _cachedSelectorResult5 = new();
	private readonly ISelector<S5> _selector5;
	private readonly Cache<S6> _cachedSelectorResult6 = new();
	private readonly ISelector<S6> _selector6;

	public MemoizedSelector(
		ISelector<S1> selector1, ISelector<S2> selector2, ISelector<S3> selector3, ISelector<S4> selector4, ISelector<S5> selector5, ISelector<S6> selector6,
		Func<Cache<TOutput>,S1,S2,S3,S4,S5,S6, TOutput> projectorFunction,
		IEqualityComparer<TOutput> equalityComparer = null)
	{
		EqualityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		ProjectorFunction = projectorFunction;
		_selector1 = selector1;
		_selector2 = selector2;
		_selector3 = selector3;
		_selector4 = selector4;
		_selector5 = selector5;
		_selector6 = selector6;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = _selector1.Apply(input);
		var selector2Result = _selector2.Apply(input);
		var selector3Result = _selector3.Apply(input);
		var selector4Result = _selector4.Apply(input);
		var selector5Result = _selector5.Apply(input);
		var selector6Result = _selector6.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result) && _cachedSelectorResult5.ValueEquals(selector5Result) && _cachedSelectorResult6.ValueEquals(selector6Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;
		_cachedSelectorResult5.Value = selector5Result;
		_cachedSelectorResult6.Value = selector6Result;

		var projectionResult = ProjectorFunction(_cachedOutput,selector1Result,selector2Result,selector3Result,selector4Result,selector5Result,selector6Result);

		if (!EqualityComparer.Equals(projectionResult, _cachedOutput.Value))
		{
			_cachedOutput.Value = projectionResult;
		}

		return _cachedOutput.Value;
	}
}
public sealed class MemoizedSelector<S1,S2,S3,S4,S5,S6,S7,TOutput> : ISelector<TOutput>
{
	private readonly Cache<TOutput> _cachedOutput = new();
	public IEqualityComparer<TOutput> EqualityComparer { get; }
	public IObservable<TOutput> Apply(IObservable<StoreState> input) => input.Select(Apply).DistinctUntilChanged();
	public Func<Cache<TOutput>,S1,S2,S3,S4,S5,S6,S7,TOutput> ProjectorFunction { get; }

	private readonly Cache<S1> _cachedSelectorResult1 = new();
	private readonly ISelector<S1> _selector1;
	private readonly Cache<S2> _cachedSelectorResult2 = new();
	private readonly ISelector<S2> _selector2;
	private readonly Cache<S3> _cachedSelectorResult3 = new();
	private readonly ISelector<S3> _selector3;
	private readonly Cache<S4> _cachedSelectorResult4 = new();
	private readonly ISelector<S4> _selector4;
	private readonly Cache<S5> _cachedSelectorResult5 = new();
	private readonly ISelector<S5> _selector5;
	private readonly Cache<S6> _cachedSelectorResult6 = new();
	private readonly ISelector<S6> _selector6;
	private readonly Cache<S7> _cachedSelectorResult7 = new();
	private readonly ISelector<S7> _selector7;

	public MemoizedSelector(
		ISelector<S1> selector1, ISelector<S2> selector2, ISelector<S3> selector3, ISelector<S4> selector4, ISelector<S5> selector5, ISelector<S6> selector6, ISelector<S7> selector7,
		Func<Cache<TOutput>,S1,S2,S3,S4,S5,S6,S7, TOutput> projectorFunction,
		IEqualityComparer<TOutput> equalityComparer = null)
	{
		EqualityComparer = equalityComparer ?? EqualityComparer<TOutput>.Default;
		ProjectorFunction = projectorFunction;
		_selector1 = selector1;
		_selector2 = selector2;
		_selector3 = selector3;
		_selector4 = selector4;
		_selector5 = selector5;
		_selector6 = selector6;
		_selector7 = selector7;
	}

	public TOutput Apply(StoreState input)
	{
		var selector1Result = _selector1.Apply(input);
		var selector2Result = _selector2.Apply(input);
		var selector3Result = _selector3.Apply(input);
		var selector4Result = _selector4.Apply(input);
		var selector5Result = _selector5.Apply(input);
		var selector6Result = _selector6.Apply(input);
		var selector7Result = _selector7.Apply(input);

		if (_cachedOutput.HasValue && _cachedSelectorResult1.ValueEquals(selector1Result) && _cachedSelectorResult2.ValueEquals(selector2Result) && _cachedSelectorResult3.ValueEquals(selector3Result) && _cachedSelectorResult4.ValueEquals(selector4Result) && _cachedSelectorResult5.ValueEquals(selector5Result) && _cachedSelectorResult6.ValueEquals(selector6Result) && _cachedSelectorResult7.ValueEquals(selector7Result))
		{
			return _cachedOutput.Value;
		}

		_cachedSelectorResult1.Value = selector1Result;
		_cachedSelectorResult2.Value = selector2Result;
		_cachedSelectorResult3.Value = selector3Result;
		_cachedSelectorResult4.Value = selector4Result;
		_cachedSelectorResult5.Value = selector5Result;
		_cachedSelectorResult6.Value = selector6Result;
		_cachedSelectorResult7.Value = selector7Result;

		var projectionResult = ProjectorFunction(_cachedOutput,selector1Result,selector2Result,selector3Result,selector4Result,selector5Result,selector6Result,selector7Result);

		if (!EqualityComparer.Equals(projectionResult, _cachedOutput.Value))
		{
			_cachedOutput.Value = projectionResult;
		}

		return _cachedOutput.Value;
	}
}

