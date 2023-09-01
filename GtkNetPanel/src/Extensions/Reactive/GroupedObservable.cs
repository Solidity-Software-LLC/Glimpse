using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GtkNetPanel.Extensions.Reactive;

public sealed class GroupedObservable<TKey, TValue> : ObservableBase<TValue>, IGroupedObservable<TKey, TValue>
{
	private readonly IObservable<TValue> _subject;

	public GroupedObservable(TKey key, ISubject<TValue> subject)
	{
		Key = key;
		_subject = subject;
	}

	public TKey Key { get; }

	protected override IDisposable SubscribeCore(IObserver<TValue> observer)
	{
		return _subject.Subscribe(observer);
	}
}
