using System.Collections.Immutable;

namespace Glimpse.State;

public interface IKeyed<TKey>
{
	public TKey Id { get; }
}
public record DataTable<TKey, TValue>
{
	public ImmutableDictionary<TKey, TValue> ById { get; init; } = ImmutableDictionary<TKey, TValue>.Empty;
	public ImmutableList<TKey> AllIds { get; init; } = ImmutableList<TKey>.Empty;

	public DataTable<TKey, TValue> UpsertOne(TKey key, TValue val)
	{
		return this with { ById = ById.SetItem(key, val), AllIds = ById.ContainsKey(key) ? AllIds : AllIds.Add(key) };
	}

	public DataTable<TKey, TValue> UpsertOne<T>(T val) where T : IKeyed<TKey>, TValue
	{
		return this with { ById = ById.SetItem(val.Id, val), AllIds = ById.ContainsKey(val.Id) ? AllIds : AllIds.Add(val.Id) };
	}

	public DataTable<TKey, TValue> UpsertMany<T>(IEnumerable<T> items) where T : IKeyed<TKey>, TValue
	{
		return items.Aggregate(this, (x, y) => x.UpsertOne(y));
	}

	public DataTable<TKey, TValue> UpsertMany(IEnumerable<KeyValuePair<TKey, TValue>> items)
	{
		return items.Aggregate(this, (x, y) => x.UpsertOne(y.Key, y.Value));
	}

	public DataTable<TKey, TValue> UpsertMany(IEnumerable<Tuple<TKey, TValue>> items)
	{
		return items.Aggregate(this, (x, y) => x.UpsertOne(y.Item1, y.Item2));
	}

	public DataTable<TKey, TValue> UpsertMany(IEnumerable<(TKey, TValue)> items)
	{
		return items.Aggregate(this, (x, y) => x.UpsertOne(y.Item1, y.Item2));
	}

	public DataTable<TKey, TValue> Remove(TKey key)
	{
		return this with { ById = ById.Remove(key), AllIds = AllIds.Remove(key) };
	}

	public DataTable<TKey, TValue> Remove<T>(T val) where T : IKeyed<TKey>, TValue
	{
		return this with { ById = ById.Remove(val.Id), AllIds = AllIds.Remove(val.Id) };
	}

	public bool ContainsKey(TKey key)
	{
		return ById.ContainsKey(key);
	}

	public IEnumerable<TValue> GetMany(IEnumerable<TKey> keys)
	{
		foreach (var k in keys)
		{
			yield return ById[k];
		}
	}

	public TValue Get(TKey key)
	{
		return ById[key];
	}

	public virtual bool Equals(DataTable<TKey, TValue> other) => ReferenceEquals(this, other);
}
