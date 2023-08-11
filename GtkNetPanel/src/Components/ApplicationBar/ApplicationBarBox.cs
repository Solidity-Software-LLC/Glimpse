using Fluxor;
using Gdk;
using Gtk;
using GtkNetPanel.State;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationBarBox : Box
{
	private readonly IState<TasksState> _tasksState;

	public ApplicationBarBox(IState<TasksState> tasksState)
	{
		_tasksState = tasksState;

		_tasksState.ToObservable().Subscribe(tasks =>
		{
			foreach (var task in tasks.Tasks)
			{
				var biggestIcon = task.Icons.MaxBy(i => i.Width);
				var imageBuffer = new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
				var image = new Image(imageBuffer.ScaleSimple(48, 48, InterpType.Bilinear));
				image.SetSizeRequest(52, 52);
				PackStart(image, false, false, 2);
			}
		});
	}
}
