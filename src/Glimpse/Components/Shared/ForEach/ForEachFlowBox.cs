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
	private readonly ObservableProperty<bool> _disableDragAndDrop = new(false);

	public IObservable<(string, int)> OrderingChanged => _orderingChangedSubject;
	public IObservable<TWidget> DragBeginObservable => _dragBeginSubject;

	public IObservable<bool> DisableDragAndDrop
	{
		get => _disableDragAndDrop;
		set => _disableDragAndDrop.UpdateSource(value);
	}

	public ForEachFlowBox(IObservable<IList<TViewModel>> itemsObservable, Func<TViewModel, string> trackBy, Func<IObservable<TViewModel>, TWidget> widgetFactory)
	{
		_dragIconTargetName = Guid.NewGuid().ToString();
		_dragTargets = new(new[] { new TargetEntry(_dragIconTargetName, TargetFlags.Widget, 0) });
		_draggingPlaceholderWidget.Data[ForEachDataKeys.Index] = 0;
		Add(_draggingPlaceholderWidget);
		SortFunc = SortByItemIndex;

		Drag.DestSet(this, 0, null, DragAction.Move);
		Drag.DestSetTargetList(this, _dragTargets);

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
				.ObserveEvent<DragBeginArgs>(nameof(flowBoxChild.DragBegin))
				.Subscribe(_ => OnDragBeginInternal(flowBoxChild));

			flowBoxChild
				.ObserveEvent(nameof(flowBoxChild.DragEnd))
				.Subscribe(_ => OnDragEndInternal(flowBoxChild));

			flowBoxChild
				.ObserveEvent<DragFailedArgs>(nameof(flowBoxChild.DragFailed))
				.Subscribe(e => e.RetVal = true);

			DisableDragAndDrop.Subscribe(b => ToggleDragSource(flowBoxChild, b));

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

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_disableDragAndDrop.Dispose();
		}

		base.Dispose(disposing);
	}

	protected override void OnShown()
	{
		base.OnShown();
		_draggingPlaceholderWidget.Visible = false;
	}

	private void ToggleDragSource(FlowBoxChild flowBoxChild, bool disabledDragAndDrop)
	{
		if (disabledDragAndDrop)
		{
			Drag.SourceUnset(flowBoxChild);
		}
		else
		{
			Drag.SourceSet(flowBoxChild, ModifierType.Button1Mask, null, DragAction.Move);
			Drag.SourceSetTargetList(flowBoxChild, _dragTargets);
		}
	}

	private void OnDragBeginInternal(FlowBoxChild flowBoxChild)
	{
		_draggingPlaceholderWidget.Data[ForEachDataKeys.Index] = flowBoxChild.Index;
		_draggingPlaceholderWidget.Visible = true;
		flowBoxChild.Visible = false;
		InvalidateSort();
		_dragBeginSubject.OnNext(flowBoxChild.Child as TWidget);
	}

	private void OnDragEndInternal(FlowBoxChild flowBoxChild)
	{
		var relativeIndex = Array.FindIndex(Children.Where(c => c.IsMapped).ToArray(), c => c == _draggingPlaceholderWidget);

		var newIndex = _draggingPlaceholderWidget.Index;
		if (newIndex < flowBoxChild.Index) newIndex--;
		flowBoxChild.Data[ForEachDataKeys.Index] = newIndex;
		flowBoxChild.Visible = true;

		_draggingPlaceholderWidget.Visible = false;
		_orderingChangedSubject.OnNext((flowBoxChild.Child.Data[ForEachDataKeys.Uri].ToString().TryGetValidFilePath(), relativeIndex));
		InvalidateSort();
	}

	protected override bool OnDragMotion(DragContext context, int x, int y, uint time)
	{
		if (context.ListTargets().All(t => t.Name != _dragIconTargetName)) return false;
		var hoveringOverFlowBoxChild = GetChildAtPos(x, y);
		if (hoveringOverFlowBoxChild == null) return false;

		if (hoveringOverFlowBoxChild != _draggingPlaceholderWidget)
		{
			var newIndex = hoveringOverFlowBoxChild.Index;
			if (newIndex < _draggingPlaceholderWidget.Index) newIndex--;
			_draggingPlaceholderWidget.Data[ForEachDataKeys.Index] = newIndex;
			InvalidateSort();
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
