using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Glimpse.Extensions.Reactive;
using Gtk;
using Drag = Gtk.Drag;

namespace Glimpse.Extensions.Gtk;

public interface IForEachDraggable
{
	IObservable<Pixbuf> IconWhileDragging { get; }
}

public enum DragResult
{
	Unknown,
	Success,
	Failed
}

public enum ForEachDataKeys
{
	Key,
	Index,
	Model
}

public static class ForEach
{
	public static ForEach<TViewModel, TKey, TWidget> Create<TViewModel, TKey, TWidget>(
		IObservable<IList<TViewModel>> itemsObservable,
		Func<TViewModel, TKey> trackBy,
		Func<IObservable<TViewModel>, TKey, TWidget> widgetFactory)
			where TKey : IEquatable<TKey>
			where TWidget : Widget, IForEachDraggable
	{
		return new ForEach<TViewModel, TKey, TWidget>(itemsObservable, trackBy, widgetFactory);
	}

	public static TViewModel GetViewModel<TViewModel, TKey, TWidget>(
		this ForEach<TViewModel, TKey, TWidget> forEachWidget,
		FlowBoxChild child)
		where TKey : IEquatable<TKey>
		where TWidget : Widget, IForEachDraggable
		where TViewModel : class
	{
		return child.Data[ForEachDataKeys.Model] as TViewModel;
	}

	public static int SortByItemIndex(FlowBoxChild child1, FlowBoxChild child2)
	{
		var index1 = (int)child1.Data[ForEachDataKeys.Index];
		var index2 = (int)child2.Data[ForEachDataKeys.Index];
		return index1.CompareTo(index2);
	}
}

public class ForEach<TViewModel, TKey, TWidget> : FlowBox
	where TKey : IEquatable<TKey>
	where TWidget : Widget, IForEachDraggable
{
	private readonly Subject<TWidget> _dragBeginObservable = new();

	public IObservable<(TKey, int)> OrderingChanged { get; }
	public IObservable<TWidget> DragBeginObservable => _dragBeginObservable;

	public ForEach(IObservable<IList<TViewModel>> itemsObservable, Func<TViewModel, TKey> trackBy, Func<IObservable<TViewModel>, TKey, TWidget> widgetFactory)
	{
		Drag.DestSet(this, 0, null, 0);
		SortFunc = ForEach.SortByItemIndex;

		var draggingPlaceholderWidget = new FlowBoxChild()
		{
			Valign = Align.Fill,
			Halign = Align.Fill,
			Visible = true,
			AppPaintable = true,
		}.AddClass("foreach__dragging-placeholder");

		var dragStatus = DragResult.Unknown;

		var orderingChangedObs = this.ObserveEvent<DragDropArgs>(nameof(DragDrop))
			.Do(_ => dragStatus = DragResult.Success)
			.Select(e =>
			{
				var sourceWidget = Drag.GetSourceWidget(e.Context).Parent as FlowBoxChild;
				var index = this.ReplaceChild(draggingPlaceholderWidget, sourceWidget);
				return ((TKey)sourceWidget.Data[ForEachDataKeys.Key], index);
			})
			.Publish();

		OrderingChanged = orderingChangedObs;
		orderingChangedObs.Connect();

		this.ObserveEvent<DragMotionArgs>(nameof(DragMotion)).Subscribe(e =>
		{
			var flowBoxChild = GetChildAtPos(e.X, e.Y);
			if (flowBoxChild == null) return;
			var index = Array.FindIndex(Children, c => c == flowBoxChild);

			if (flowBoxChild != draggingPlaceholderWidget)
			{
				if (draggingPlaceholderWidget.Parent != null) Remove(draggingPlaceholderWidget);
				Insert(draggingPlaceholderWidget, index);
			}

			Gdk.Drag.Status(e.Context, e.Context.SuggestedAction, e.Time);
			e.RetVal = true;
		});

		itemsObservable.UnbundleMany(trackBy).Subscribe(itemObservable =>
		{
			var childWidget = widgetFactory(itemObservable.Select(i => i.Item1).DistinctUntilChanged(), itemObservable.Key);
			childWidget.Data[ForEachDataKeys.Key] = itemObservable.Key;

			var flowBoxChild = new FlowBoxChild().AddMany(childWidget);
			flowBoxChild.Data[ForEachDataKeys.Index] = 0;
			flowBoxChild.Data[ForEachDataKeys.Key] = itemObservable.Key;
			Add(flowBoxChild);
			flowBoxChild.ShowAll();

			childWidget
				.ObserveEvent<DragBeginArgs>(nameof(childWidget.DragBegin))
				.WithLatestFrom(childWidget.IconWhileDragging)
				.Subscribe(t =>
				{
					if (t.Second != null) Drag.SourceSetIconPixbuf(childWidget, t.Second);
					_dragBeginObservable.OnNext(childWidget);
					dragStatus = DragResult.Unknown;
					SortFunc = null;
					draggingPlaceholderWidget.SetSizeRequest(childWidget.WidthRequest, childWidget.HeightRequest);
					this.ReplaceChild(flowBoxChild, draggingPlaceholderWidget);
				});

			childWidget
				.ObserveEvent<DragEndArgs>(nameof(childWidget.DragEnd))
				.Where(_ => dragStatus != DragResult.Success)
				.Subscribe(_ =>
				{
					Remove(draggingPlaceholderWidget);
					Insert(flowBoxChild, (int) flowBoxChild.Data[ForEachDataKeys.Index]);
					SortFunc =  ForEach.SortByItemIndex;
				});

			childWidget
				.ObserveEvent<DragFailedArgs>(nameof(childWidget.DragFailed))
				.Do(_ => dragStatus = DragResult.Failed)
				.Subscribe(e => e.RetVal = true);

			Drag.SourceSet(childWidget, ModifierType.Button1Mask, null, DragAction.Move);

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
}
