namespace Glimpse.Redux.Reducers;

public interface IFeatureReducer
{
	IEnumerable<ActionReducer<StoreState>> ActionReducers { get; }
	StoreState InitializeStore(StoreState state);
}
