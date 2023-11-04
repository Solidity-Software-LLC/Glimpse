namespace Glimpse.State.StateLens;

public static class Reducers
{
	public static List<On<TState>> CombineReducers<TState>(
		params List<On<TState>>[] reducersList)
			where TState : class
	{
		var result = new List<On<TState>>();

		foreach (var reducers in reducersList)
		{
			result.AddRange(reducers);
		}

		return result;
	}

	public static StateLens<TState, TFeatureState> CreateSubReducers<TState, TFeatureState>(
		Func<TState, TFeatureState> featureSelector,
		Func<TState, TFeatureState, TState> stateReducer)
			where TState : class, new()
			where TFeatureState : class, new()
	{
		return new StateLens<TState, TFeatureState>(featureSelector, stateReducer);
	}

	public static On<TState> On<TAction, TState>(
		Func<TState, TAction, TState> reducer)
			where TState : class
			where TAction : class
	{
		return new On<TState>
		{
			Reduce = (state, action) =>
			{
				if (!(action is TAction actionT1))
					return state;

				return reducer(state, actionT1);
			},
			Types = new[] { typeof(TAction).FullName }
		};
	}
}
