using Cairo;
using Gdk;
using Gtk;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationIcon : EventBox
{
	private IconGroupViewModel _currentViewModel;

	public ApplicationIcon(IObservable<IconGroupViewModel> viewModel)
	{
		Vexpand = false;
		Valign = Align.Center;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));

		EnterNotifyEvent += (_, _) =>
		{
			SetStateFlags(StateFlags.Prelight, true);
			QueueDraw();
		};

		LeaveNotifyEvent += (_, _) =>
		{
			SetStateFlags(StateFlags.Normal, true);
			QueueDraw();
		};

		viewModel.Subscribe(group =>
		{
			Children.ToList().ForEach(c => Remove(c));
			_currentViewModel = group;
			var task = group.Tasks.First();
			var biggestIcon = task.Icons.MaxBy(i => i.Width);
			var imageBuffer = new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
			var image = new Image(imageBuffer.ScaleSimple(26, 26, InterpType.Bilinear));
			image.SetSizeRequest(42, 42);
			Add(image);
			ShowAll();
			QueueDraw();
		});
	}

	protected override bool OnDrawn(Context cr)
	{
		cr.Save();

		var w = Window.Width;
		var h = Window.Height;

		if (_currentViewModel.Tasks.Count > 1)
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
			imageContext.SetSourceRGBA(1, 1, 1, StateFlags.HasFlag(StateFlags.Prelight) ? 0.3 : 0.1);
			imageContext.RoundedRectangle(0, 0, w, h, 4);
			imageContext.Fill();

			cr.SetSourceSurface(imageSurface, 0, 0);
			cr.Paint();

			imageSurface.Dispose();
			imageContext.Dispose();
		}
		else
		{
			cr.Operator = Operator.Over;
			cr.SetSourceRGBA(1, 1, 1, StateFlags.HasFlag(StateFlags.Prelight) ? 0.3 : 0.1);
			cr.RoundedRectangle(0, 0, w, h, 4);
			cr.Fill();
		}

		cr.Restore();
		return base.OnDrawn(cr);
	}
}
