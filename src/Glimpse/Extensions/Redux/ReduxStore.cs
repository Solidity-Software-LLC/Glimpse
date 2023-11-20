using System.Reactive.Linq;
using System.Reactive.Subjects;
using Glimpse.Extensions.Redux.Effects;
using Glimpse.Extensions.Redux.Reducers;
using Glimpse.Extensions.Redux.Selectors;

namespace Glimpse.Extensions.Redux;

public sealed class ReduxStore
{
	private readonly TaskScheduler _actionTaskScheduler;
	private readonly Subject<object> _actionDispatcher = new();
	private readonly List<ActionReducer<StoreState>> _reducers = new();
	private readonly BehaviorSubject<StoreState> _stateSubject;
	private readonly Queue<Tuple<TaskCompletionSource, object>> _actionQueue = new();
	private readonly object _lock = new();

	public ReduxStore(FeatureReducerCollection[] features, TaskScheduler dispatchedActionScheduler = null)
	{
		State = new StoreState();

		foreach (var f in features.SelectMany(f => f))
		{
			RegisterReducers(f);
		}

		_stateSubject = new BehaviorSubject<StoreState>(State);
		_actionTaskScheduler = dispatchedActionScheduler ?? new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
	}

	public StoreState State { get; private set; }

	public Task Dispatch(object action)
	{
		if (action == null) return Task.CompletedTask;

		var task = new TaskCompletionSource();

		lock (_lock)
		{
			_actionQueue.Enqueue(Tuple.Create(task, action));
			if (_actionQueue.Count > 1) return task.Task;
		}

		Task.Factory.StartNew(ProcessActionQueue, CancellationToken.None, TaskCreationOptions.None, _actionTaskScheduler);
		return task.Task;
	}

	private void ProcessActionQueue()
	{
		while (true)
		{
			Tuple<TaskCompletionSource, object> t = null;

			lock (_lock)
			{
				if (_actionQueue.Count == 0) break;
				t = _actionQueue.Dequeue();
			}

			UpdateState(Reduce(State, t.Item2));
			_actionDispatcher.OnNext(t.Item2);
			t.Item1.SetResult();
		}
	}

	public IObservable<object> ObserveAction()
	{
		return _actionDispatcher;
	}

	public IObservable<T> ObserveAction<T>()
	{
		return _actionDispatcher.OfType<T>();
	}

	public void RegisterEffects(params Effect[] effects)
	{
		effects
			.Where(effect => effect.Run != null && effect.Config != null)
			.Select(effect => effect.Config.Dispatch
				? effect.Run(this).Retry()
				: effect.Run(this).Retry().Select(_ => Observable.Empty<object>()))
			.Merge()
			.Subscribe(a => Dispatch(a));
	}

	public void RegisterReducers(params IFeatureReducer[] reducers)
	{
		foreach (var r in reducers)
		{
			State = r.InitializeStore(State);
			_reducers.AddRange(r.ActionReducers);
		}
	}

	private StoreState Reduce(StoreState state, object action)
	{
		var actionName = action.GetType().FullName;
		var currentState = state;

		foreach (var reducer in _reducers)
		{
			if (reducer.ActionType.Contains(actionName))
			{
				currentState = reducer.Reduce(currentState, action);
			}
		}

		return currentState;
	}

	private void UpdateState(StoreState state)
	{
		State = state;
		_stateSubject.OnNext(State);
	}

	public IObservable<TResult> Select<TResult>(Func<StoreState, TResult> selector)
	{
		return _stateSubject.Select(selector).DistinctUntilChanged();
	}

	public IObservable<TResult> Select<TResult>(ISelector<TResult> selector)
	{
		return selector.Apply(_stateSubject);
	}
}
