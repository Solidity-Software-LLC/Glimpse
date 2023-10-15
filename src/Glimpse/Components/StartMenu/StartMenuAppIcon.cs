using System.Reactive.Linq;
using Gdk;
using Glimpse.Components.Shared.ForEach;
using Glimpse.Extensions.Gtk;
using Glimpse.Services.FreeDesktop;
using Gtk;
using Pango;
using WrapMode = Pango.WrapMode;

namespace Glimpse.Components.StartMenu;

public class StartMenuAppIcon : EventBox, IForEachDraggable
{
	public StartMenuAppIcon(IObservable<StartMenuAppViewModel> viewModelObservable)
	{
		CanFocus = false;

		this.AddClass("start-menu__app-icon-container");

		var name = new Label();
		name.Ellipsize = EllipsizeMode.End;
		name.Lines = 2;
		name.LineWrap = true;
		name.LineWrapMode = WrapMode.Word;
		name.MaxWidthChars = 1;
		name.Justify = Justification.Center;

		var image = new Image();
		image.SetSizeRequest(36, 36);

		var appIconContainer = new Box(Orientation.Vertical, 0);
		appIconContainer.AddMany(image, name);
		appIconContainer.Valign = Align.Center;
		appIconContainer.Halign = Align.Center;

		Add(appIconContainer);

		ContextMenuRequested = this.CreateContextMenuObservable()
			.TakeUntilDestroyed(this)
			.WithLatestFrom(viewModelObservable)
			.Select(t => t.Second.DesktopFile);

		viewModelObservable
			.TakeUntilDestroyed(this)
			.Subscribe(f => name.Text = f.DesktopFile.Name);

		var iconNameObs = viewModelObservable.Select(vm => vm.DesktopFile.IconName).DistinctUntilChanged();
		var iconTheme = IconTheme.GetForScreen(Screen);
		var iconThemeChanged = iconTheme.ObserveChange().WithLatestFrom(viewModelObservable).Select(t => t.Second.DesktopFile.IconName);
		var iconObservable = iconNameObs.Merge(iconThemeChanged).Select(f => (iconTheme.LoadIcon(f, 36), iconTheme.LoadIcon(f, 30))).Replay(1);

		this.AppIcon(image, iconObservable);
		iconObservable.Connect();

		IconWhileDragging = iconObservable.Select(t => t.Item1);
	}

	public IObservable<DesktopFile> ContextMenuRequested { get; }
	public IObservable<Pixbuf> IconWhileDragging { get; }
}
