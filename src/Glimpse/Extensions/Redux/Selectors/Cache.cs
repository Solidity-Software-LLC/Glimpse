namespace Glimpse.Extensions.Redux.Selectors;

public class Cache<T>
{
	private T _value;

	public bool HasValue;

	public T Value
	{
		get => _value;
		set
		{
			_value = value;
			HasValue = true;
		}
	}

	public bool ValueEquals(T other)
	{
		if (!HasValue)
		{
			return false;
		}

		return EqualityComparer<T>.Default.Equals(other, _value);
	}
}
