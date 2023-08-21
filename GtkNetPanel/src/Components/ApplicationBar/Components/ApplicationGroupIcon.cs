using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cairo;
using Gdk;
using Gtk;
using GtkNetPanel.Components.ContextMenu;
using GtkNetPanel.Components.Shared;

namespace GtkNetPanel.Components.ApplicationBar.Components;

public class ApplicationGroupIcon : EventBox
{
	private ApplicationBarGroupViewModel _currentViewModel;
	private readonly Subject<bool> _contextMenuObservable = new();

	public ApplicationGroupIcon(IObservable<ApplicationBarGroupViewModel> viewModel)
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

		var image = new Image();
		image.SetSizeRequest(42, 42);
		Add(image);
		ShowAll();

		viewModel.DistinctUntilChanged(x => x.Tasks.Count).Subscribe(group =>
		{
			_currentViewModel = group;
			Pixbuf imageBuffer;

			if (!string.IsNullOrEmpty(group.DesktopFile.IconName))
			{
				if (group.DesktopFile.IconName.StartsWith("/"))
				{
					imageBuffer = new Pixbuf(File.ReadAllBytes(group.DesktopFile.IconName));
				}
				else
				{
					imageBuffer = IconTheme.GetForScreen(Screen).LoadIcon(group.DesktopFile.IconName, 26, IconLookupFlags.DirLtr);
				}
			}
			else if (group.Tasks.Count > 0)
			{
				var task = group.Tasks.First();
				var biggestIcon = task.Icons.MaxBy(i => i.Width);
				imageBuffer = new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
			}
			else
			{
				imageBuffer = IconTheme.GetForScreen(Screen).LoadIcon("application-default-icon", 26, IconLookupFlags.DirLtr);
			}

			imageBuffer = imageBuffer.ScaleSimple(26, 26, InterpType.Bilinear);
			image.Pixbuf = imageBuffer;

			QueueDraw();
		});
	}

	public IObservable<bool> ContextMenuOpened => _contextMenuObservable;

	protected override bool OnDrawn(Context cr)
	{
		if (_currentViewModel == null) return base.OnDrawn(cr);

		cr.Save();

		var w = Window.Width;
		var h = Window.Height;

		if (_currentViewModel.Tasks.Count == 0)
		{
			cr.Operator = Operator.Over;
			cr.SetSourceRGBA(1, 1, 1, StateFlags.HasFlag(StateFlags.Prelight) ? 0.3 : 0);
			cr.RoundedRectangle(0, 0, w, h, 4);
			cr.Fill();
		}
		else if (_currentViewModel.Tasks.Count == 1)
		{
			cr.Operator = Operator.Over;
			cr.SetSourceRGBA(1, 1, 1, StateFlags.HasFlag(StateFlags.Prelight) ? 0.3 : 0.1);
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
			imageContext.SetSourceRGBA(1, 1, 1, StateFlags.HasFlag(StateFlags.Prelight) ? 0.3 : 0.1);
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
