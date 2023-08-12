using Fluxor;
using Gdk;
using Gtk;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarBox : Box
{
	public ApplicationBarBox(IState<TasksState> tasksState)
	{
		tasksState.ToObservable().Subscribe(tasks =>
		{
			foreach (var task in tasks.Tasks)
			{
				PackStart(CreateAppIcon(task), false, false, 2);
			}
		});
	}

	private EventBox CreateAppIcon(TaskState task)
	{
		var biggestIcon = task.Icons.MaxBy(i => i.Width);
		var imageBuffer = new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
		var image = new Image(imageBuffer.ScaleSimple(28, 28, InterpType.Bilinear));
		image.SetSizeRequest(42, 42);

		var eventBox = new EventBox();
		eventBox.Vexpand = false;
		eventBox.Valign = Align.Center;
		eventBox.StyleContext.AddClass("highlight");
		eventBox.StyleContext.AddClass("application-icon");
		eventBox.SetSizeRequest(28, 28);
		eventBox.AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));
		eventBox.Add(image);
		eventBox.EnterNotifyEvent += (_, _) => eventBox.SetStateFlags(StateFlags.Prelight, true);
		eventBox.LeaveNotifyEvent += (_, _) => eventBox.SetStateFlags(StateFlags.Normal, true);
		eventBox.ButtonReleaseEvent += (o, args) =>
		{
			var matchingWindow = Window.Display.DefaultScreen.WindowStack.First(w => w.GetIntProperty(Atoms._NET_WM_PID) == task.ProcessId);
			var states = matchingWindow.GetAtomProperty(Atom.Intern("_NET_WM_STATE", true));

			if (states.Any(a => a.Name == "_NET_WM_STATE_HIDDEN"))
			{
				matchingWindow.Raise();
			}
			else if (states.All(a => a.Name != "_NET_WM_STATE_FOCUSED"))
			{
				matchingWindow.Focus(0);
			}
			else
			{
				matchingWindow.Iconify();
			}
		};
		return eventBox;
	}
}
