using Gdk;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Services.FreeDesktop;
using Pango;
using WrapMode = Pango.WrapMode;

namespace GtkNetPanel.Components.ApplicationMenu;

public class ApplicationMenuAppIcon : EventBox
{
	public ApplicationMenuAppIcon(DesktopFile desktopFile)
	{
		var name = new Label(desktopFile.Name);
		name.SetSizeRequest(82, 16);
		name.Ellipsize = EllipsizeMode.End;
		name.Lines = 2;
		name.LineWrap = true;
		name.LineWrapMode = WrapMode.Word;
		name.MaxWidthChars = 1;
		name.Justify = Justification.Center;

		var appIconContainer = new Box(Orientation.Vertical, 0);
		appIconContainer.PackStart(new Image(CreateAppIcon(desktopFile.IconName).ScaleSimple(36, 36, InterpType.Bilinear)), false, false, 0);
		appIconContainer.PackStart(name, false, false, 0);
		appIconContainer.Valign = Align.Center;
		appIconContainer.Halign = Align.Center;

		Add(appIconContainer);
		this.AddHoverHighlighting();
		SetSizeRequest(90, 76);
		StyleContext.AddClass("app-menu__app-icon");
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
