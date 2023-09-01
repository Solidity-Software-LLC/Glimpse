using System.Reactive.Linq;
using Tmds.DBus.Protocol;

namespace GtkNetPanel.Services.DBus.Core;

public static class SignalHelper
{
	public static ValueTask<IDisposable> WatchSignalAsync(Connection connection, MatchRule rule, Action<Exception?> handler, bool emitOnCapturedContext = true) => connection.AddMatchAsync(rule, static (_, _) => null !, static (Exception e, object _, object? _, object? handlerState) => ((Action<Exception?>)handlerState!).Invoke(e), null, handler, emitOnCapturedContext);

	public static ValueTask<IDisposable> WatchSignalAsync<T>(Connection connection, MatchRule rule, MessageValueReader<T> reader, Action<Exception?, T> handler, bool emitOnCapturedContext = true) => connection.AddMatchAsync(rule, reader, static (e, arg, readerState, handlerState) => ((Action<Exception?, T>)handlerState!).Invoke(e, arg), null, handler, emitOnCapturedContext);

	public static ValueTask<IDisposable> WatchPropertiesChangedAsync<T>(Connection connection, string destination, string path, string @interface, MessageValueReader<PropertyChanges<T>> reader, Action<Exception?, PropertyChanges<T>> handler, bool emitOnCapturedContext = true)
	{
		var rule = new MatchRule
		{
			Type = MessageType.Signal,
			Sender = destination,
			Path = path,
			Member = "PropertiesChanged",
			Interface = "org.freedesktop.DBus.Properties",
			Arg0 = @interface
		};
		return WatchSignalAsync(connection, rule, reader, handler, emitOnCapturedContext);
	}

	public static IObservable<T> WatchSignal<T>(this Connection connection, MatchRule matchRule, MessageValueReader<T> reader)
	{
		return Observable.Create<T>(async obs =>
		{
			return await WatchSignalAsync(
				connection,
				matchRule,
				reader,
				(e, t) =>
				{
					if (e != null)
						obs.OnError(e);
					else
						obs.OnNext(t);
				},
				false);
		});
	}
}
