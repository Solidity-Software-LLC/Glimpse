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
	public StartMenuAppIcon(IObservable<StartMenuAppViewModel> desktopFileObservable)
	{
		CanFocus = false;

		this.AddHoverHighlighting();
		this.AddClass("start-menu__app-icon-container");

		var name = new Label();
		name.Ellipsize = EllipsizeMode.End;
		name.Lines = 2;
		name.LineWrap = true;
		name.LineWrapMode = WrapMode.Word;
		name.MaxWidthChars = 1;
		name.Justify = Justification.Center;

		var image = new Image();

		var appIconContainer = new Box(Orientation.Vertical, 0);
		appIconContainer.AddMany(image, name);
		appIconContainer.Valign = Align.Center;
		appIconContainer.Halign = Align.Center;

		Add(appIconContainer);

		ContextMenuRequested = this.CreateContextMenuObservable()
			.TakeUntilDestroyed(this)
			.WithLatestFrom(desktopFileObservable)
			.Select(t => t.Second.DesktopFile);

		desktopFileObservable
			.TakeUntilDestroyed(this)
			.Subscribe(f => name.Text = f.DesktopFile.Name);

		var iconObservable = desktopFileObservable
			.Select(f => IconLoader.LoadIcon(f.DesktopFile.IconName, 36) ?? IconLoader.DefaultAppIcon(36))
			.Replay(1);

		iconObservable.Subscribe(pixbuf => image.Pixbuf = pixbuf);
		iconObservable.Connect();

		IconWhileDragging = iconObservable;
	}

	public IObservable<DesktopFile> ContextMenuRequested { get; }
	public IObservable<Pixbuf> IconWhileDragging { get; }
}
