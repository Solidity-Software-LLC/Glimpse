using System.Diagnostics;
using Gdk;
using Gtk;
using GtkNetPanel.Components.Shared;

namespace GtkNetPanel.Components.ApplicationMenuButton;

public class AppMenu : EventBox
{
	public AppMenu()
	{
		var imageBuffer = IconTheme.GetForScreen(Screen).LoadIcon("ubuntu-logo-icon", 28, IconLookupFlags.DirLtr);
		imageBuffer = imageBuffer.ScaleSimple(28, 28, InterpType.Bilinear);

		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		CanFocus = false;
		StyleContext.AddClass("application-icon");
		Add(new Image(imageBuffer));
		SetSizeRequest(42, 42);
		this.AddHoverHighlighting();

		ButtonReleaseEvent += (_, _) =>
		{
			var startInfo = new ProcessStartInfo() { FileName = "/usr/bin/xfce4-popup-whiskermenu", Arguments = "-p"};
			Process.Start(startInfo);
		};
	}
}
