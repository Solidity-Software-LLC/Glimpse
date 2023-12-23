using System.Collections.Immutable;

namespace Glimpse.Redux;

public class StoreState
{
	private ImmutableDictionary<Type, object> _featureDictionary = ImmutableDictionary<Type, object>.Empty;

	public T GetFeatureState<T>() where T : class
	{
		return _featureDictionary[typeof(T)] as T;
	}

	public StoreState UpdateFeatureState<T>(T state) where T : class
	{
		return new StoreState() { _featureDictionary = _featureDictionary.SetItem(typeof(T), state) };
	}

	public bool Equals(StoreState other)
	{
		return ReferenceEquals(this, other);
	}
}
