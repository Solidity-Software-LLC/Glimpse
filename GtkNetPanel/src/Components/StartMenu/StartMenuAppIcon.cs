using System.Reactive.Linq;
using Gtk;
using GtkNetPanel.Extensions.Gtk;
using GtkNetPanel.Services.FreeDesktop;
using Pango;
using WrapMode = Pango.WrapMode;

namespace GtkNetPanel.Components.StartMenu;

public class StartMenuAppIcon : Button
{
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
		appIconContainer.AddMany(image, name);
		appIconContainer.Valign = Align.Center;
		appIconContainer.Halign = Align.Center;

		Add(appIconContainer);
		StyleContext.AddClass("start-menu__app-icon");
		ContextMenuRequested = this.CreateContextMenuObservable().WithLatestFrom(desktopFileObservable).Select(t => t.Second);

		desktopFileObservable
			.TakeUntilDestroyed(this)
			.Subscribe(desktopFile =>
			{
				name.Text = desktopFile.Name;
				image.Pixbuf = IconLoader.LoadIcon(desktopFile.IconName, 36) ?? IconLoader.DefaultAppIcon(36);
			});
	}

	public IObservable<DesktopFile> ContextMenuRequested { get; }
}
