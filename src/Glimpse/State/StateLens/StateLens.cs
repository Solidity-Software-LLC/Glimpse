namespace Glimpse.State.StateLens;

public class StateLens<TState, TFeatureState>
	where TState : class, new()
	where TFeatureState : class, new()
{
	private readonly Func<TState, TFeatureState> _featureSelector;
	private readonly Func<TState, TFeatureState, TState> _stateReducer;
	private readonly List<On<TState>> _ons = new();

	public StateLens(
		Func<TState, TFeatureState> featureSelector,
		Func<TState, TFeatureState, TState> stateReducer
	)
	{
		_featureSelector = featureSelector;
		_stateReducer = stateReducer;
	}

	public StateLens<TState, TFeatureState> On<TAction>(Func<TFeatureState, TAction, TFeatureState> featureReducer) where TAction : class
	{
		_ons.Add(
			CreateParentReducer(Reducers.On(featureReducer))
		);
		return this;
	}

	public List<On<TState>> ToList()
	{
		return _ons;
	}

	public static implicit operator List<On<TState>>(StateLens<TState, TFeatureState> lens)
	{
		return lens.ToList();
	}

	private On<TState> CreateParentReducer(On<TFeatureState> on)
	{
		return new On<TState>
		{
			Reduce = (state, action) =>
			{
				if (on?.Reduce == null)
					return state;

				var featureState = _featureSelector(state);
				var reducerResult = on.Reduce(featureState, action);

				if (featureState.Equals(reducerResult))
					return state;

				return _stateReducer(state, reducerResult);
			},
			Types = on.Types
		};
	}
}
