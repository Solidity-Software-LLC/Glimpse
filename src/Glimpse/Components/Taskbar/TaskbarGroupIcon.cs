using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cairo;
using Gdk;
using Glimpse.Extensions.Gtk;
using Gtk;

namespace Glimpse.Components.Taskbar;

public class TaskbarGroupIcon : EventBox
{
	private TaskbarGroupViewModel _currentViewModel;
	private readonly Subject<bool> _contextMenuObservable = new();
	private readonly Subject<EventButton> _buttonRelease = new();

	public TaskbarGroupIcon(IObservable<TaskbarGroupViewModel> viewModel)
	{
		Visible = false;
		Vexpand = false;
		Valign = Align.Center;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		this.CreateContextMenuObservable().Subscribe(_ => _contextMenuObservable.OnNext(true));
		AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));

		this.ObserveEvent(nameof(EnterNotifyEvent)).Subscribe(_ =>
		{
			SetStateFlags(StateFlags.Prelight, true);
			QueueDraw();
		});

		this.ObserveEvent(nameof(LeaveNotifyEvent)).Subscribe(_ =>
		{
			SetStateFlags(StateFlags.Normal, true);
			QueueDraw();
		});

		SetSizeRequest(42, 42);
		var image = new Image();
		Add(image);
		ShowAll();

		viewModel.DistinctUntilChanged(x => x.Tasks.Count).Subscribe(group =>
		{
			_currentViewModel = group;

			image.Pixbuf = IconLoader.LoadIcon(group.DesktopFile.IconName, 26)
				?? IconLoader.LoadIcon(group.Tasks.FirstOrDefault(), 26)
				?? IconLoader.DefaultAppIcon(26);

			QueueDraw();
		});
	}

	protected override bool OnButtonReleaseEvent(EventButton evnt)
	{
		_buttonRelease.OnNext(evnt);
		return true;
	}

	public IObservable<bool> ContextMenuOpened => _contextMenuObservable;
	public IObservable<EventButton> ButtonRelease => _buttonRelease;

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
