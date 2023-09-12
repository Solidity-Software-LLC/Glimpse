using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cairo;
using Gdk;
using Glimpse.Extensions.Reactive;
using Gtk;
using Drag = Gtk.Drag;

namespace Glimpse.Extensions.Gtk;

public class ForEach<T, TKey> : Box where TKey : IEquatable<TKey>
{
	private int? _dragXPosition;
	private readonly Subject<(TKey, int)> _orderingChanged = new();

	public ForEach(IObservable<IList<T>> itemsObservable, Func<T, TKey> trackBy, Func<IObservable<T>, Widget> widgetFactory)
	{
		MarginStart = 4;
		MarginEnd = 4;
		Spacing = 4;
		Orientation = Orientation.Horizontal;
		Drag.DestSet(this, 0, null, 0);

		itemsObservable.UnbundleMany(trackBy).Subscribe(itemObservable =>
		{
			var groupIcon = widgetFactory(itemObservable.Select(i => i.Item1).DistinctUntilChanged());
			groupIcon.Data["Key"] = itemObservable.Key;

			Drag.SourceSet(groupIcon, ModifierType.Button1Mask, null, DragAction.Move);
			Add(groupIcon);
			ShowAll();

			itemObservable.Subscribe(
				t => ReorderChild(groupIcon, t.Item2),
				_ => { },
				() => { Remove(groupIcon); });
		});
	}

	public IObservable<(TKey, int)> OrderingChanged => _orderingChanged;

	protected override void OnDragLeave(DragContext context, uint time)
	{
		_dragXPosition = null;
	}

	protected override void OnDragEnd(DragContext context)
	{
		_dragXPosition = null;
		QueueDraw();
	}

	protected override bool OnDragMotion(DragContext context, int x, int y, uint time)
	{
		_dragXPosition = x;
		QueueDraw();
		Gdk.Drag.Status(context, context.SuggestedAction, time);
		return true;
	}

	protected override bool OnDragDrop(DragContext context, int x, int y, uint time)
	{
		var sourceWidget = Drag.GetSourceWidget(context);
		var oldIndex = Children.ToList().IndexOf(sourceWidget);
		var newIndex = FindIndexOfDrop(x);
		if (newIndex > oldIndex) newIndex--;

		_orderingChanged.OnNext(((TKey) sourceWidget.Data["Key"], newIndex));
		_dragXPosition = null;
		QueueDraw();
		return base.OnDragDrop(context, x, y, time);
	}

	private int FindIndexOfDrop(int x)
	{
		var childWidget = this.FindChildAtX(x);
		var firstChild = Children.First();
		var lastChild = Children.Last();

		firstChild.TranslateCoordinates(this, 0, 0, out var firstChildX, out _);
		lastChild.TranslateCoordinates(this, 0, 0, out var lastChildX, out _);

		if (x <= firstChildX) childWidget = firstChild;
		else if (x >= lastChildX + lastChild.Allocation.Width) childWidget = lastChild;

		childWidget.TranslateCoordinates(this, 0, 0, out var left, out _);
		var childIndex = Children.ToList().FindIndex(w => w == childWidget);

		var iconWidth = childWidget.Allocation.Width;
		var center = left + iconWidth / 2;
		return x > center ? childIndex + 1 : childIndex;
	}

	protected override bool OnDrawn(Context cr)
	{
		if (_dragXPosition.HasValue)
		{
			var childWidget = this.FindChildAtX(_dragXPosition.Value);
			var firstChild = Children.First();
			var lastChild = Children.Last();

			firstChild.TranslateCoordinates(this, 0, 0, out var firstChildX, out _);
			lastChild.TranslateCoordinates(this, 0, 0, out var lastChildX, out _);

			if (_dragXPosition.Value <= firstChildX) childWidget = firstChild;
			else if (_dragXPosition.Value >= lastChildX + lastChild.Allocation.Width) childWidget = lastChild;

			childWidget.TranslateCoordinates(this, 0, 0, out var left, out _);
			var iconWidth = childWidget.Allocation.Width;
			var right = left + iconWidth;
			var center = left + iconWidth / 2;
			var lineXPosition = _dragXPosition > center ? right + 2 : left - 2;

			var lineHeight = childWidget.HeightRequest + 4;
			var marginTop = (Allocation.Height - lineHeight) / 2;

			cr.MoveTo(lineXPosition, marginTop);
			cr.LineTo(lineXPosition, Allocation.Height - marginTop);
			cr.SetSourceRGBA(1, 1, 1, 0.6);
			cr.Stroke();
		}

		return base.OnDrawn(cr);
	}
}
