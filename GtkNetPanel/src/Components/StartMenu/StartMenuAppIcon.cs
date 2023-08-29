using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services.FreeDesktop;
using Pango;
using WrapMode = Pango.WrapMode;

namespace GtkNetPanel.Components.StartMenu;

public class StartMenuAppIcon : EventBox
{
	private readonly Subject<DesktopFile> _contextMenuRequested = new();

	public StartMenuAppIcon(IObservable<DesktopFile> desktopFileObservable)
	{
		var name = new Label();
		name.SetSizeRequest(76, 16);
		name.Ellipsize = EllipsizeMode.End;
		name.Lines = 2;
		name.LineWrap = true;
		name.LineWrapMode = WrapMode.Word;
		name.MaxWidthChars = 1;
		name.Justify = Justification.Center;

		var image = new Image();

		var appIconContainer = new Box(Orientation.Vertical, 0);
		appIconContainer.PackStart(image, false, false, 0);
		appIconContainer.PackStart(name, false, false, 0);
		appIconContainer.Valign = Align.Center;
		appIconContainer.Halign = Align.Center;

		Add(appIconContainer);
		this.AddHoverHighlighting();
		SetSizeRequest(82, 76);
		StyleContext.AddClass("start-menu__app-icon");

		this.CreateContextMenuObservable()
			.WithLatestFrom(desktopFileObservable)
			.Subscribe(t => _contextMenuRequested.OnNext(t.Second));

		desktopFileObservable
			.TakeUntilDestroyed(this)
			.Subscribe(desktopFile =>
			{
				name.Text = desktopFile.Name;
				image.Pixbuf = IconLoader.LoadIcon(desktopFile.IconName, 36) ?? IconLoader.DefaultAppIcon(36);
			});
	}

	public IObservable<DesktopFile> ContextMenuRequested => _contextMenuRequested;
}
