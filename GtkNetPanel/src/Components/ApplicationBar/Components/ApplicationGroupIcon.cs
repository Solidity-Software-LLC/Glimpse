using System.Reactive.Subjects;
using Cairo;
using Gdk;
using Gtk;
using GtkNetPanel.Components.ContextMenu;

namespace GtkNetPanel.Components.ApplicationBar.Components;

public class ApplicationGroupIcon : EventBox
{
	private IconGroupViewModel _currentViewModel;
	private readonly Subject<bool> _contextMenuObservable = new();

	public ApplicationGroupIcon(IObservable<IconGroupViewModel> viewModel)
	{
		Visible = false;

		Vexpand = false;
		Valign = Align.Center;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;
		var contextMenuHelper = new ContextMenuHelper(this);
		AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));

		contextMenuHelper.ContextMenu += (_, _) =>
		{
			_contextMenuObservable.OnNext(true);
		};

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
			Children.ToList().ForEach(Remove);
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

	public IObservable<bool> ContextMenuOpened => _contextMenuObservable;

	protected override bool OnDrawn(Context cr)
	{
		cr.Save();

		var w = Window.Width;
		var h = Window.Height;

		if (_currentViewModel != null && _currentViewModel.Tasks.Count > 1)
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
