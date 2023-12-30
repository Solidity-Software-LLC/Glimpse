using System.Reactive.Linq;
using Gdk;
using Glimpse.Common.Gtk;
using Glimpse.Common.Images;
using Glimpse.UI.State;
using Gtk;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse.UI;

public static class GtkExtensions
{
	public static void BindViewModel(this Image image, IObservable<ImageViewModel> imageViewModel, int size)
	{
		image.BindViewModel(imageViewModel, size, size);
	}

	private static void SetByName(this Image image, ImageViewModel vm, int width, int height)
	{
		if (vm.IconName.StartsWith("/"))
		{
			image.Pixbuf = new Pixbuf(vm.IconName).ScaleToFit(width, height);
		}
		else
		{
			image.SetFromIconName(vm.IconName, IconSize.LargeToolbar);
			image.PixelSize = width;
		}
	}

	private static void SetImage(this Image image, ImageViewModel vm, int width, int height)
	{
		image.Pixbuf = vm.Image.ScaleToFit(width, height).Image;
	}

	public static void BindViewModel(this Image image, IObservable<ImageViewModel> imageViewModel, int width, int height)
	{
		imageViewModel.Subscribe(vm =>
		{
			if (vm.Image != null) image.SetImage(vm, width, height);
			else if (!string.IsNullOrEmpty(vm.IconName)) image.SetByName(vm, width, height);
			else vm.IconName = Guid.NewGuid().ToString();
		});
	}

	public static void AppIcon(this Widget widget, Image image, IObservable<ImageViewModel> iconObservable, int size)
	{
		iconObservable.Subscribe(vm =>
		{
			if (vm.Image != null)
			{
				image.Data["Small"] = vm.Image.Scale(size - 6);
				image.Data["Big"] = vm.Image.Scale(size);
			}
			else if (vm.IconName.StartsWith("/"))
			{
				var glimpseImage = GlimpseImageFactory.From(new Pixbuf(vm.IconName, size, size));
				image.Pixbuf = glimpseImage.Image;
				image.Data["Small"] = glimpseImage.Scale(size - 6);
				image.Data["Big"] = glimpseImage;
			}
		});

		image.BindViewModel(iconObservable, size);
		widget.AddButtonStates();
		widget.ObserveEvent(w => w.Events().EnterNotifyEvent).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(w => w.Events().LeaveNotifyEvent).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(w => w.Events().ButtonPressEvent).Subscribe(_ => widget.QueueDraw());
		widget.ObserveEvent(w => w.Events().ButtonPressEvent).WithLatestFrom(iconObservable).Subscribe(t =>
		{
			if (image.Pixbuf == null) image.PixelSize = size - 6;
			else image.Pixbuf = ((GtkGlimpseImage)image.Data["Small"])?.Image;
		});
		widget.ObserveEvent(w => w.Events().ButtonReleaseEvent).WithLatestFrom(iconObservable).Subscribe(t =>
		{
			if (image.Pixbuf == null) image.PixelSize = size;
			else image.Pixbuf = ((GtkGlimpseImage)image.Data["Big"])?.Image;
		});
		widget.ObserveEvent(w => w.Events().LeaveNotifyEvent).WithLatestFrom(iconObservable).Subscribe(t =>
		{
			if (image.Pixbuf == null) image.PixelSize = size;
			else image.Pixbuf = ((GtkGlimpseImage)image.Data["Big"])?.Image;
		});
	}
}
