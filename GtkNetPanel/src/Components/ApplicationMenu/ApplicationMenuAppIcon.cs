using Gdk;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Components.Shared.ContextMenu;
using GtkNetPanel.Services.FreeDesktop;
using Pango;
using WrapMode = Pango.WrapMode;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuAppIcon : EventBox
{
	public ApplicationMenuAppIcon(IObservable<DesktopFile> desktopFileObservable)
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
		StyleContext.AddClass("app-menu__app-icon");

		var contextMenu = new Menu();
		contextMenu.Add(new MenuItem("Pin to Start"));
		contextMenu.Add(new MenuItem("Pin to taskbar"));
		contextMenu.ShowAll();

		var helper = new ContextMenuHelper(this);
		helper.ContextMenu += (sender, args) => contextMenu.Popup();

		desktopFileObservable.Subscribe(desktopFile =>
		{
			name.Text = desktopFile.Name;
			image.Pixbuf = CreateAppIcon(desktopFile.IconName).ScaleSimple(36, 36, InterpType.Bilinear);
		});
	}

	private Pixbuf CreateAppIcon(string iconName)
	{
		var iconTheme = IconTheme.GetForScreen(Screen);

		if (!string.IsNullOrEmpty(iconName))
		{
			if (iconName.StartsWith("/"))
			{
				return new Pixbuf(File.ReadAllBytes(iconName));
			}

			if (iconTheme.HasIcon(iconName))
			{
				return iconTheme.LoadIcon(iconName, 64, IconLookupFlags.DirLtr);
			}
		}

		return iconTheme.LoadIcon("application-default-icon", 64, IconLookupFlags.DirLtr);
	}
}
