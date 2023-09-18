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

public class ForEach<TViewModel, TKey, TWidget> : FlowBox where TKey : IEquatable<TKey> where TWidget : Widget, IForEachDraggable
{
	private const string ForEachKey = "ForEachKey";
	private const string ForEachIndex = "ForEachIndex";
	private readonly Subject<TWidget> _dragBeginObservable = new();

	public IObservable<(TKey, int)> OrderingChanged { get; }
	public IObservable<TWidget> DragBeginObservable => _dragBeginObservable;

	public ForEach(IObservable<IList<TViewModel>> itemsObservable, Func<TViewModel, TKey> trackBy, Func<IObservable<TViewModel>, TWidget> widgetFactory)
	{
		MarginStart = 4;
		MaxChildrenPerLine = 100;
		MinChildrenPerLine = 100;
		RowSpacing = 0;
		ColumnSpacing = 4;
		Orientation = Orientation.Horizontal;
		Homogeneous = false;
		Valign = Align.Center;
		Halign = Align.Start;
		SelectionMode = SelectionMode.None;
		SortFunc = SortItems;
		Expand = false;

		Drag.DestSet(this, 0, null, 0);

		var draggingPlaceholderWidget = new FlowBoxChild()
		{
			Valign = Align.Center,
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
				return ((TKey)sourceWidget.Data[ForEachKey], index);
			})
			.Publish();

		OrderingChanged = orderingChangedObs;
		orderingChangedObs.Connect();

		this.ObserveEvent<DragMotionArgs>(nameof(DragMotion)).Subscribe(e =>
		{
			var (flowBoxChild, index) = this.FindChildAtX(e.X);

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
			var childWidget = widgetFactory(itemObservable.Select(i => i.Item1).DistinctUntilChanged());
			childWidget.Data[ForEachKey] = itemObservable.Key;
			var flowBoxChild = childWidget.Parent as FlowBoxChild;

			if (flowBoxChild == null)
			{
				flowBoxChild = new FlowBoxChild().AddMany(childWidget);
				flowBoxChild.Data[ForEachIndex] = 0;
				flowBoxChild.Data[ForEachKey] = itemObservable.Key;
				flowBoxChild.ShowAll();
				Add(flowBoxChild);
			}

			childWidget
				.ObserveEvent<DragBeginArgs>(nameof(childWidget.DragBegin))
				.Subscribe(e =>
				{
					_dragBeginObservable.OnNext(childWidget);
					dragStatus = DragResult.Unknown;
					SortFunc = null;
					draggingPlaceholderWidget.Margin = childWidget.Margin;
					draggingPlaceholderWidget.SetSizeRequest(childWidget.WidthRequest, childWidget.HeightRequest);
					this.ReplaceChild(flowBoxChild, draggingPlaceholderWidget);
				});

			childWidget
				.ObserveEvent<DragEndArgs>(nameof(childWidget.DragEnd))
				.Where(_ => dragStatus != DragResult.Success)
				.Subscribe(_ =>
				{
					Remove(draggingPlaceholderWidget);
					Insert(flowBoxChild, (int) flowBoxChild.Data[ForEachIndex]);
				});

			childWidget
				.ObserveEvent<DragFailedArgs>(nameof(childWidget.DragFailed))
				.Do(_ => dragStatus = DragResult.Failed)
				.Subscribe(e => e.RetVal = true);

			childWidget.IconWhileDragging
				.Subscribe(icon => Drag.SourceSetIconPixbuf(childWidget, icon));

			Drag.SourceSet(childWidget, ModifierType.Button1Mask, null, DragAction.Move);

			itemObservable
				.Select(i => i.Item2)
				.DistinctUntilChanged()
				.Subscribe(i =>
					{
						flowBoxChild.Data[ForEachIndex] = i;
						SortFunc = SortItems;
						InvalidateSort();
					},
					_ => { },
					() => Remove(childWidget));
		});
	}

	private int SortItems(FlowBoxChild child1, FlowBoxChild child2)
	{
		var child1Index = (int)child1.Data[ForEachIndex];
		var child2Index = (int)child2.Data[ForEachIndex];
		return child1Index.CompareTo(child2Index);
	}
}
