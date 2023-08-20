using Gtk;

namespace GtkNetPanel.Components.Shared;

public class ForEach<T> : Box
{
	public ForEach(IObservable<IObservable<T>> itemsObservable, Func<IObservable<T>, Widget> widgetFactory)
	{
		itemsObservable.Subscribe(itemObservable =>
		{
			var groupIcon = widgetFactory(itemObservable);
			PackStart(groupIcon, false, false, 2);
			ShowAll();

			itemObservable.Subscribe(
				_ => { },
				_ => { },
				() => { Remove(groupIcon); });
		});
	}
}
