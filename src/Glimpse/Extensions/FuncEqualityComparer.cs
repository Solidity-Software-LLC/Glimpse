namespace Glimpse.Extensions;

public class FuncEqualityComparer<T> : IEqualityComparer<T>
{
	private readonly Func<T, T, bool> _comparison;

	public FuncEqualityComparer(Func<T, T, bool> comparison)
	{
		_comparison = comparison;
	}

	public bool Equals(T x, T y) => _comparison(x, y);
	public int GetHashCode(T obj) => obj.GetHashCode();
}
