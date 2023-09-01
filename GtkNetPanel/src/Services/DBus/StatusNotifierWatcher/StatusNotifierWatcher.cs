using System.Reactive.Subjects;
using GtkNetPanel.Services.DBus.Interfaces;
using Tmds.DBus.Protocol;

namespace GtkNetPanel.Services.DBus.StatusNotifierWatcher;

public class StatusNotifierWatcher : OrgKdeStatusNotifierWatcher
{
	private readonly Subject<string> _itemRegistered = new();
	private readonly Subject<string> _itemRemoved = new();

	public StatusNotifierWatcher(OrgFreedesktopDBus dbusInterface, Connection connection) : base(true)
	{
		Connection = connection;

		dbusInterface.NameChanged.Subscribe(t =>
		{
			var matchingItem = BackingProperties.RegisteredStatusNotifierItems.FirstOrDefault(s => s == t.Item1 && string.IsNullOrEmpty(t.Item3));

			if (!string.IsNullOrEmpty(matchingItem))
			{
				BackingProperties.RegisteredStatusNotifierItems = BackingProperties.RegisteredStatusNotifierItems.Where(s => s != matchingItem).ToArray();
				_itemRemoved.OnNext(matchingItem);
				EmitStatusNotifierItemUnregistered(matchingItem);
			}
		});
	}

	protected override Connection Connection { get; }
	public override string Path { get; } = "/StatusNotifierWatcher";
	public IObservable<string> ItemRegistered => _itemRegistered;
	public IObservable<string> ItemRemoved => _itemRemoved;

	public void RegisterStatusNotifierHostAsync(string service)
	{
		BackingProperties.IsStatusNotifierHostRegistered = true;
		EmitStatusNotifierHostRegistered();
	}

	protected override ValueTask OnRegisterStatusNotifierHostAsync(string service)
	{
		return ValueTask.CompletedTask;
	}

	protected override ValueTask OnRegisterStatusNotifierItemAsync(string sender, string service)
	{
		BackingProperties.RegisteredStatusNotifierItems = BackingProperties.RegisteredStatusNotifierItems.Concat(new[] { sender }).ToArray();
		EmitStatusNotifierItemRegistered(sender);
		_itemRegistered.OnNext(sender);
		return ValueTask.CompletedTask;
	}
}
