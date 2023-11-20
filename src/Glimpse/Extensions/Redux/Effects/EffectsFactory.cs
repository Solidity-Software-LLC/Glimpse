using System.Reactive.Linq;
using Glimpse.Extensions.Redux.Selectors;

namespace Glimpse.Extensions.Redux.Effects;

public static class EffectsFactory
{
	public static Effect CreateEffect<TAction>(Action<TAction> action)
		where TAction : class
	{
		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.Do(action),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1>(ISelector<TSelected1> selector1, Action<TAction, TSelected1> action)
		where TAction : class
	{
		var nullObject = new object();

		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.WithLatestFrom(store.Select(selector1))
				.Do(tuple => action(tuple.Item1, tuple.Item2))
				.Select(_ => nullObject),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1, TSelected2>(
		ISelector<TSelected1> selector1,
		ISelector<TSelected2> selector2,
		Action<TAction, TSelected1, TSelected2> action)
		where TAction : class
	{
		var nullObject = new object();

		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1), store.Select(selector2))
				.Do(t => action(t.Item1, t.Item2, t.Item3))
				.Select(_ => nullObject),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1, TSelected2, TSelected3>(
		ISelector<TSelected1> selector1,
		ISelector<TSelected2> selector2,
		ISelector<TSelected3> selector3,
		Action<TAction, TSelected1, TSelected2, TSelected3> action)
		where TAction : class
	{
		var nullObject = new object();

		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1), store.Select(selector2), store.Select(selector3))
				.Do(t => action(t.Item1, t.Item2, t.Item3, t.Item4))
				.Select(_ => nullObject),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction>(Func<TAction, Task> f)
		where TAction : class
	{
		var nullObject = new object();

		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.Select(a => Observable.FromAsync(() => f(a)))
				.Concat()
				.Select(_ => nullObject),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1>(ISelector<TSelected1> selector1, Func<TAction, TSelected1, Task> action)
		where TAction : class
	{
		var nullObject = new object();

		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1))
				.Select(tuple => Observable.FromAsync(() => action(tuple.Item1, tuple.Item2)))
				.Concat()
				.Select(_ => nullObject),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1, TSelected2>(
		ISelector<TSelected1> selector1,
		ISelector<TSelected2> selector2,
		Func<TAction, TSelected1, TSelected2, Task> action)
		where TAction : class
	{
		var nullObject = new object();

		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1), store.Select(selector2))
				.Select(tuple => Observable.FromAsync(() => action(tuple.Item1, tuple.Item2, tuple.Item3)))
				.Concat()
				.Select(_ => nullObject),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1, TSelected2, TSelected3>(
		ISelector<TSelected1> selector1,
		ISelector<TSelected2> selector2,
		ISelector<TSelected3> selector3,
		Func<TAction, TSelected1, TSelected2, TSelected3, Task> action)
		where TAction : class
	{
		var nullObject = new object();

		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1), store.Select(selector2), store.Select(selector3))
				.Select(tuple => Observable.FromAsync(() => action(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4)))
				.Concat()
				.Select(_ => nullObject),
			Config = new EffectConfig { Dispatch = false }
		};
	}

	public static Effect CreateEffect<TAction>(Func<TAction, IEnumerable<object>> f)
		where TAction : class
	{
		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.SelectMany(f),
			Config = new EffectConfig { Dispatch = true }
		};
	}

	public static Effect CreateEffect<TAction>(Func<TAction, Task<IEnumerable<object>>> f)
		where TAction : class
	{
		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.Select(a => Observable.FromAsync(() => f(a)))
				.Concat()
				.SelectMany(x => x),
			Config = new EffectConfig { Dispatch = true }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1>(ISelector<TSelected1> selector1, Func<TAction, TSelected1, IEnumerable<object>> f)
		where TAction : class
	{
		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.WithLatestFrom(store.Select(selector1))
				.SelectMany(t => f(t.Item1, t.Item2)),
			Config = new EffectConfig { Dispatch = true }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1, TSelected2>(
		ISelector<TSelected1> selector1,
		ISelector<TSelected2> selector2,
		Func<TAction, TSelected1, TSelected2, IEnumerable<object>> f)
		where TAction : class
	{
		return new Effect
		{
			Run = store => store.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1), store.Select(selector2))
				.SelectMany(t => f(t.Item1, t.Item2, t.Item3)),
			Config = new EffectConfig { Dispatch = true }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1>(
		ISelector<TSelected1> selector1,
		Func<TAction, TSelected1, Task<IEnumerable<object>>> f)
		where TAction : class
	{
		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1))
				.Select(s => Observable.FromAsync(() => f(s.First, s.Second)))
				.Concat()
				.SelectMany(x => x),
			Config = new EffectConfig { Dispatch = true }
		};
	}

	public static Effect CreateEffect<TAction, TSelected1, TSelected2>(
		ISelector<TSelected1> selector1,
		ISelector<TSelected2> selector2,
		Func<TAction, TSelected1, TSelected2, Task<IEnumerable<object>>> f)
		where TAction : class
	{
		return new Effect
		{
			Run = store => store
				.ObserveAction<TAction>()
				.CombineLatest(store.Select(selector1), store.Select(selector2))
				.Select(s => Observable.FromAsync(() => f(s.Item1, s.Item2, s.Item3)))
				.Concat()
				.SelectMany(x => x),
			Config = new EffectConfig { Dispatch = true }
		};
	}

	public static Effect CreateEffect(Func<ReduxStore, IObservable<object>> run, bool dispatch = false)
	{
		return new Effect
		{
			Run = run,
			Config = new EffectConfig { Dispatch = dispatch }
		};
	}
}
