using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cairo;
using Gdk;
using Glimpse.Components.Shared.ForEach;
using Glimpse.Extensions.Gtk;
using Gtk;
using Color = Cairo.Color;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.Components.Taskbar;

public class TaskbarGroupIcon : EventBox, IForEachDraggable
{
	private readonly TaskbarWindowPicker _taskbarWindowPicker;
	private SlotViewModel _currentViewModel;

	public IObservable<Pixbuf> IconWhileDragging { get; }

	public TaskbarGroupIcon(IObservable<SlotViewModel> viewModel, TaskbarWindowPicker taskbarWindowPicker)
	{
		_taskbarWindowPicker = taskbarWindowPicker;
		Visible = false;
		Expand = false;
		Valign = Align.Fill;
		Halign = Align.Fill;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;
		this.AddClass("taskbar__group-icon");

		var image = new Image();
		Add(image);
		ShowAll();

		viewModel.Subscribe(vm => _currentViewModel = vm);
		viewModel.Select(vm => vm.DemandsAttention).DistinctUntilChanged().Subscribe(_ => QueueDraw());

		var iconObservable = viewModel
			.Select(vm => vm.Icon)
			.DistinctUntilChanged()
			.CombineLatest(this.ObserveEvent(w => w.Events().SizeAllocated).DistinctUntilChanged(a => a.Allocation.Width))
			.Select(t => (t.First.Scale(26), t.First.Scale(20)))
			.Where(i => i.Item1 != null)
			.TakeUntil(viewModel.TakeLast(1))
			.Publish();

		this.AppIcon(image, iconObservable);
		this.ObserveEvent(w => w.Events().ButtonReleaseEvent).Subscribe(e => e.RetVal = true);
		IconWhileDragging = iconObservable.Select(t => t.Item1);

		iconObservable.Connect();
	}

	public void CloseWindowPicker()
	{
		_taskbarWindowPicker.ClosePopup();
	}

	protected override bool OnDrawn(Context cr)
	{
		if (_currentViewModel == null) return base.OnDrawn(cr);

		cr.Save();

		var w = Window.Width;
		var h = Window.Height;

		var demandsAttention = _currentViewModel.DemandsAttention;
		var backgroundAlpha = StateFlags.HasFlag(StateFlags.Prelight) ? 0.3
			: _currentViewModel.Tasks.Count > 0 ? 0.1
			: 0;

		var backgroundColor = demandsAttention ? new Color(1, 1, 0, backgroundAlpha + 0.4) : new Color(1, 1, 1, backgroundAlpha);

		if (_currentViewModel.Tasks.Count == 0)
		{
			cr.Operator = Operator.Over;
			cr.SetSourceColor(backgroundColor);
			cr.RoundedRectangle(0, 0, w, h, 4);
			cr.Fill();
		}
		else if (_currentViewModel.Tasks.Count == 1)
		{
			cr.Operator = Operator.Over;
			cr.SetSourceColor(backgroundColor);
			cr.RoundedRectangle(0, 0, w, h, 4);
			cr.Fill();
		}
		else
		{
			var imageSurface = new ImageSurface(Format.ARGB32, w, h);
			var imageContext = new Context(imageSurface);

			imageContext.Operator = Operator.Over;
			imageContext.SetSourceRGBA(1, 0, 0, 1);
			imageContext.RoundedRectangle(0, 0, w-5, h, 4);
			imageContext.Fill();

			imageContext.Operator = Operator.Out;
			imageContext.SetSourceRGBA(0, 1, 0, 1);
			imageContext.RoundedRectangle(0, 0, w-2, h, 4);
			imageContext.Fill();

			imageContext.Operator = Operator.Out;
			imageContext.SetSourceColor(backgroundColor);
			imageContext.RoundedRectangle(0, 0, w, h, 4);
			imageContext.Fill();

			cr.SetSourceSurface(imageSurface, 0, 0);
			cr.Paint();

			imageSurface.Dispose();
			imageContext.Dispose();
		}

		cr.Restore();
		return base.OnDrawn(cr);
	}
}
