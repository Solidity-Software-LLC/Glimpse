using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Glimpse.Extensions.Gtk;
using Glimpse.Extensions.Reactive;
using Gtk;
using Drag = Gtk.Drag;

namespace Glimpse.Components.Shared.ForEach;

public class ForEachFlowBox<TViewModel, TWidget> : FlowBox where TWidget : Widget, IForEachDraggable
{
	private readonly Subject<(string, int)> _orderingChangedSubject = new();
	private readonly Subject<TWidget> _dragBeginSubject = new();
	private readonly FlowBoxChild _draggingPlaceholderWidget = new FlowBoxChild() { Visible = true }.AddClass("foreach__dragging-placeholder");
	private readonly string _dragIconTargetName;
	private readonly TargetList _dragTargets;

	public IObservable<(string, int)> OrderingChanged => _orderingChangedSubject;
	public IObservable<TWidget> DragBeginObservable => _dragBeginSubject;

	public ForEachFlowBox(IObservable<IList<TViewModel>> itemsObservable, Func<TViewModel, string> trackBy, Func<IObservable<TViewModel>, TWidget> widgetFactory)
	{
		_dragIconTargetName = Guid.NewGuid().ToString();
		_dragTargets = new(new[] { new TargetEntry(_dragIconTargetName, TargetFlags.Widget, 0) });

		Drag.DestSet(this, 0, null, DragAction.Move);
		Drag.DestSetTargetList(this, _dragTargets);
		SortFunc = SortByItemIndex;

		itemsObservable.UnbundleMany(trackBy).Subscribe(itemObservable =>
		{
			var childWidget = widgetFactory(itemObservable.Select(i => i.Item1).DistinctUntilChanged());
			var flowBoxChild = new FlowBoxChild().AddMany(childWidget);
			flowBoxChild.Data[ForEachDataKeys.Index] = 0;
			Add(flowBoxChild);
			flowBoxChild.ShowAll();

			flowBoxChild
				.ObserveEvent<DragBeginArgs>(nameof(flowBoxChild.DragBegin))
				.WithLatestFrom(childWidget.IconWhileDragging)
				.Subscribe(t => Drag.SourceSetIconPixbuf(flowBoxChild, t.Second));

			flowBoxChild
				.ObserveEvent<DragFailedArgs>(nameof(flowBoxChild.DragFailed))
				.Subscribe(e => e.RetVal = OnDragIconFailed(flowBoxChild));

			Drag.SourceSet(flowBoxChild, ModifierType.Button1Mask, null, DragAction.Move);
			Drag.SourceSetTargetList(flowBoxChild, _dragTargets);

			itemObservable
				.Select(i => i.Item1)
				.DistinctUntilChanged()
				.Subscribe(i => flowBoxChild.Data[ForEachDataKeys.Model] = i);

			itemObservable
				.Select(i => i.Item2)
				.DistinctUntilChanged()
				.Subscribe(i => flowBoxChild.Data[ForEachDataKeys.Index] = i, _ => { }, () => Remove(flowBoxChild));
		});

		itemsObservable.Subscribe(_ =>
		{
			InvalidateSort();
			InvalidateFilter();
		});
	}

	private bool OnDragIconFailed(FlowBoxChild flowBoxChild)
	{
		return OnDragIconDrop(flowBoxChild);
	}

	protected override bool OnDragDrop(DragContext context, int x, int y, uint time)
	{
		return OnDragIconDrop(Drag.GetSourceWidget(context) as FlowBoxChild);
	}

	private bool OnDragIconDrop(FlowBoxChild flowBoxChild)
	{
		var relativeIndex = Array.FindIndex(Children.Where(c => c.IsMapped).ToArray(), c => c == _draggingPlaceholderWidget);
		flowBoxChild.Data[ForEachDataKeys.Index] = _draggingPlaceholderWidget.Index;
		Insert(flowBoxChild, _draggingPlaceholderWidget.Index);
		_orderingChangedSubject.OnNext((flowBoxChild.Child.Data[ForEachDataKeys.Uri].ToString().TryGetValidFilePath(), relativeIndex));
		if (_draggingPlaceholderWidget.Parent != null) Remove(_draggingPlaceholderWidget);
		SortFunc = SortByItemIndex;
		return true;
	}

	protected override bool OnDragMotion(DragContext context, int x, int y, uint time)
	{
		if (context.ListTargets().All(t => t.Name != _dragIconTargetName)) return false;
		var hoveringOverFlowBoxChild = GetChildAtPos(x, y);
		if (hoveringOverFlowBoxChild == null) return false;

		if (_draggingPlaceholderWidget.Parent == null)
		{
			SortFunc = null;
			Insert(_draggingPlaceholderWidget, hoveringOverFlowBoxChild.Index);

			if (Drag.GetSourceWidget(context) as FlowBoxChild is { Parent: not null } dragSourceWidget)
			{
				Remove(dragSourceWidget);
				_dragBeginSubject.OnNext(dragSourceWidget.Child as TWidget);
			}
		}
		else if (hoveringOverFlowBoxChild != _draggingPlaceholderWidget)
		{
			var newIndex = hoveringOverFlowBoxChild.Index;
			Remove(_draggingPlaceholderWidget);
			Insert(_draggingPlaceholderWidget, newIndex);
		}

		Gdk.Drag.Status(context, context.SuggestedAction, time);
		return true;
	}

	private int SortByItemIndex(FlowBoxChild child1, FlowBoxChild child2)
	{
		var index1 = (int)child1.Data[ForEachDataKeys.Index];
		var index2 = (int)child2.Data[ForEachDataKeys.Index];
		return index1.CompareTo(index2);
	}
}
