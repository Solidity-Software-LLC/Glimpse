using System.Reactive.Linq;
using System.Reactive.Subjects;
using Gdk;
using Gtk;
using GtkNetPanel.Components.Shared;
using GtkNetPanel.Components.Shared.ContextMenu;
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

		var helper = new ContextMenuHelper(this);

		Observable.FromEventPattern(helper, nameof(helper.ContextMenu))
			.WithLatestFrom(desktopFileObservable)
			.TakeUntilDestroyed(this)
			.Subscribe(t => _contextMenuRequested.OnNext(t.Second));

		desktopFileObservable
			.TakeUntilDestroyed(this)
			.Subscribe(desktopFile =>
			{
				name.Text = desktopFile.Name;
				image.Pixbuf = CreateAppIcon(desktopFile.IconName).ScaleSimple(36, 36, InterpType.Bilinear);
			});
	}

	public IObservable<DesktopFile> ContextMenuRequested => _contextMenuRequested;

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
