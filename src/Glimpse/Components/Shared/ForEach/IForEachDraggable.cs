using Gdk;

namespace Glimpse.Components.Shared.ForEach;

public interface IForEachDraggable
{
	IObservable<Pixbuf> IconWhileDragging { get; }
}
