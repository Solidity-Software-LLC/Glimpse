using System.Diagnostics;
using Cairo;
using Gdk;
using Gtk;

namespace GtkNetPanel;

public class AppMenu : EventBox
{
	public AppMenu()
	{
		Add(Image.NewFromIconName("xubuntu-logo-menu", IconSize.LargeToolbar));
		AddEvents((int) EventMask.ButtonPressMask);
		AddEvents((int) EventMask.EnterNotifyMask);
		AddEvents((int) EventMask.LeaveNotifyMask);
		AddEvents((int) EventMask.ScrollMask);
		ModifyBg(StateType.Normal);

		EnterNotifyEvent += (_, _) =>
		{
			SetStateFlags(StateFlags.Active, true);
			QueueDraw();
		};
		LeaveNotifyEvent += (_, _) =>
		{
			SetStateFlags(StateFlags.Normal, true);
			QueueDraw();
		};

		ButtonPressEvent += (_, _) =>
		{
			var process = new ProcessStartInfo() { FileName = "/usr/bin/xfce4-popup-whiskermenu", UseShellExecute = false, Arguments = "-p"};
			Process.Start(process);
		};
	}

	protected override bool OnDrawn(Context cr)
	{
		if (StateFlags.HasFlag(StateFlags.Active))
		{
			cr.SetSourceRGBA(255, 255, 255, 0.2);
			cr.Rectangle(0, 0, WidthRequest, HeightRequest);
			cr.Paint();
		}

		return base.OnDrawn(cr);
	}
}
