using System.Diagnostics;
using Cairo;
using Gdk;
using Gtk;

namespace GtkNetPanel.Components.ApplicationMenuButton;

public class AppMenu : EventBox
{
	public AppMenu()
	{
		var imageBuffer = IconTheme.GetForScreen(Screen).LoadIcon("ubuntu-logo-icon", 28, IconLookupFlags.DirLtr);
		imageBuffer = imageBuffer.ScaleSimple(28, 28, InterpType.Bilinear);
		Add(new Image(imageBuffer));
		SetSizeRequest(42, 42);
		Expand = false;
		Valign = Align.Center;
		Halign = Align.Center;
		AddEvents((int) EventMask.ButtonPressMask);
		AddEvents((int) EventMask.EnterNotifyMask);
		AddEvents((int) EventMask.LeaveNotifyMask);
		AddEvents((int) EventMask.ScrollMask);
		StyleContext.AddClass("highlight");
		ModifyBg(StateType.Normal);
		CanFocus = false;

		EnterNotifyEvent += (_, _) => SetStateFlags(StateFlags.Prelight, true);
		LeaveNotifyEvent += (_, _) => SetStateFlags(StateFlags.Normal, true);
		ButtonPressEvent += (_, _) =>
		{
			var process = new ProcessStartInfo() { FileName = "/usr/bin/xfce4-popup-whiskermenu", UseShellExecute = false, Arguments = "-p"};
			Process.Start(process);
		};
	}
}
