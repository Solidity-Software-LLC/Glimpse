using System.Reactive.Linq;
using Gtk;

namespace Glimpse.Extensions.Gtk;

public static class GtkDirectives
{
	public static void ForEach<TKey, TValue>(this FlowBox parent, IObservable<IGroupedObservable<TKey, TValue>> itemsObservable, Func<IGroupedObservable<TKey, TValue>, Widget> widgetFactory)
	{
		itemsObservable.Subscribe(itemObservable =>
		{
			var groupIcon = widgetFactory(itemObservable);

			if (groupIcon.Parent == null)
			{
				parent.Add(groupIcon);
				groupIcon.ShowAll();
			}

			var child = groupIcon.Parent as FlowBoxChild;
			child.Visible = true;

			itemObservable.Subscribe(
				_ => { },
				e => { Console.WriteLine(e.ToString());},
				() => { child.Visible = false; });
		});
	}
}
