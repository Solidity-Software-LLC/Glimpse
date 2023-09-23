using Gtk;

namespace Glimpse.Components.Shared.ForEach;

public static class ForEachExtensions
{
	public static ForEachFlowBox<TViewModel, TWidget> Create<TViewModel, TWidget>(
		IObservable<IList<TViewModel>> itemsObservable,
		Func<TViewModel, string> trackBy,
		Func<IObservable<TViewModel>, TWidget> widgetFactory)
			where TWidget : Widget, IForEachDraggable
	{
		return new ForEachFlowBox<TViewModel, TWidget>(itemsObservable, trackBy, widgetFactory);
	}

	public static TViewModel GetViewModel<TViewModel, TWidget>(
		this ForEachFlowBox<TViewModel, TWidget> forEachFlowBoxWidget,
		FlowBoxChild child)
		where TWidget : Widget, IForEachDraggable
		where TViewModel : class
	{
		return child.Data[ForEachDataKeys.Model] as TViewModel;
	}
}
