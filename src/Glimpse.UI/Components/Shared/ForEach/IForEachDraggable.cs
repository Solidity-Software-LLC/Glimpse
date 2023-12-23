using Glimpse.Images;

namespace Glimpse.UI.Components.Shared.ForEach;

public interface IForEachDraggable
{
	IObservable<IGlimpseImage> IconWhileDragging { get; }
}
