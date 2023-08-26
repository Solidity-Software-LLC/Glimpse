using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GtkNetPanel.Services;

public static class ReactiveExtensions
{
	public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> obs, Func<T, T, bool> comparison)
	{
		return obs.DistinctUntilChanged(new FuncEqualityComparer<T>(comparison));
	}

	public static IObservable<IGroupedObservable<TKey, IEnumerable<TValue>>> Unbundle<TValue, TKey>(this IObservable<IEnumerable<TValue>> source, Func<TValue, TKey> keySelector) where TKey : IEquatable<TKey>
	{
		return Observable.Create<IGroupedObservable<TKey, IEnumerable<TValue>>>(obs =>
		{
			var groupSubjects = new Dictionary<TKey, Subject<IEnumerable<TValue>>>();

			return source.Subscribe(elements =>
				{
					var groupings = elements.GroupBy(keySelector).ToList();

					foreach (var group in groupings)
					{
						if (!groupSubjects.ContainsKey(group.Key))
						{
							groupSubjects.Add(group.Key, new Subject<IEnumerable<TValue>>());
							obs.OnNext(new GroupedObservable<TKey, IEnumerable<TValue>>(group.Key, groupSubjects[group.Key]));
						}

						groupSubjects[group.Key].OnNext(group);
					}

					foreach (var key in groupSubjects.Keys)
					{
						if (groupings.All(e => !e.Key.Equals(key)))
						{
							groupSubjects[key].OnCompleted();
							groupSubjects.Remove(key);
						}
					}
				},
				e =>
				{
					obs.OnError(e);
					foreach (var s in groupSubjects.Values) s.OnError(e);
				},
				() =>
				{
					foreach (var s in groupSubjects.Values) s.OnCompleted();
					obs.OnCompleted();
				});
		});
	}

	public static IObservable<IGroupedObservable<TKey, TValue>> UnbundleMany<TValue, TKey>(this IObservable<IEnumerable<TValue>> source, Func<TValue, TKey> keySelector) where TKey : IEquatable<TKey>
	{
		return Observable.Create<IGroupedObservable<TKey, TValue>>(obs =>
		{
			var groupSubjects = new Dictionary<TKey, Subject<TValue>>();

			return source.Subscribe(elements =>
				{
					foreach (var e in elements)
					{
						var key = keySelector(e);

						if (!groupSubjects.ContainsKey(key))
						{
							groupSubjects.Add(key, new Subject<TValue>());
							obs.OnNext(new GroupedObservable<TKey, TValue>(key, groupSubjects[key]));
						}

						groupSubjects[key].OnNext(e);
					}

					foreach (var key in groupSubjects.Keys)
					{
						if (elements.All(e => !keySelector(e).Equals(key)))
						{
							groupSubjects[key].OnCompleted();
							groupSubjects.Remove(key);
						}
					}
				},
				e =>
				{
					obs.OnError(e);
					foreach (var s in groupSubjects.Values) s.OnError(e);
				},
				() =>
				{
					foreach (var s in groupSubjects.Values) s.OnCompleted();
					obs.OnCompleted();
				});
		});
	}
}
