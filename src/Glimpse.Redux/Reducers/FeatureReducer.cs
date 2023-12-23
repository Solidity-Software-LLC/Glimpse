namespace Glimpse.Redux.Reducers;

public static class FeatureReducer
{
	public static FeatureReducer<TFeatureState> Build<TFeatureState>(TFeatureState initialState) where TFeatureState : class
	{
		return new FeatureReducer<TFeatureState>(initialState);
	}
}

public class FeatureReducer<TFeatureState>(TFeatureState initialState) : IFeatureReducer where TFeatureState : class
{
	private readonly List<ActionReducer<TFeatureState>> _actionReducers = new();

	public FeatureReducer<TFeatureState> On<TAction>(Func<TFeatureState, TAction, TFeatureState> f) where TAction : class
	{
		_actionReducers.Add(new ActionReducer<TFeatureState>
		{
			Reduce = (state, action) => action is TAction actionT1 ? f(state, actionT1) : state,
			ActionType = typeof(TAction).FullName
		});

		return this;
	}

	public StoreState InitializeStore(StoreState state)
	{
		return state.UpdateFeatureState(initialState);
	}

	public IEnumerable<ActionReducer<StoreState>> ActionReducers => _actionReducers.Select(r => new ActionReducer<StoreState>()
	{
		Reduce = (state, action) =>
		{
			var featureState = state.GetFeatureState<TFeatureState>();
			var newFeatureState = r.Reduce(featureState, action);
			return state.UpdateFeatureState(newFeatureState);
		},
		ActionType = r.ActionType
	});
}
