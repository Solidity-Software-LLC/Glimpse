using Cairo;
using Gdk;
using Gtk;
using GtkNetPanel.Components.ContextMenu;
using GtkNetPanel.Services.DisplayServer;
using Action = System.Action;

namespace GtkNetPanel.Components.ApplicationBar;

public class ApplicationIcon : EventBox
{
	private IconGroupViewModel _currentViewModel;
	private Menu _contextMenu;

	private readonly List<AllowedWindowActions> _displayableActions = new()
	{
		AllowedWindowActions.Maximize,
		AllowedWindowActions.Minimize,
		AllowedWindowActions.Move,
		AllowedWindowActions.Resize
	};

	public ApplicationIcon(IObservable<IconGroupViewModel> viewModel)
	{
		Vexpand = false;
		Valign = Align.Center;
		AppPaintable = true;
		Visual = Screen.RgbaVisual;

		var contextMenuHelper = new ContextMenuHelper(this);
		contextMenuHelper.ContextMenu += ShowContextMenu;

		AddEvents((int)(EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask));

		EnterNotifyEvent += (_, _) =>
		{
			SetStateFlags(StateFlags.Prelight, true);
			QueueDraw();
		};

		LeaveNotifyEvent += (_, _) =>
		{
			SetStateFlags(StateFlags.Normal, true);
			QueueDraw();
		};

		viewModel.Subscribe(group =>
		{
			Children.ToList().ForEach(Remove);
			_currentViewModel = group;
			var task = group.Tasks.First();
			var biggestIcon = task.Icons.MaxBy(i => i.Width);
			var imageBuffer = new Pixbuf(biggestIcon.Data, Colorspace.Rgb, true, 8, biggestIcon.Width, biggestIcon.Height, sizeof(int) * biggestIcon.Width);
			var image = new Image(imageBuffer.ScaleSimple(26, 26, InterpType.Bilinear));
			image.SetSizeRequest(42, 42);
			Add(image);
			ShowAll();
			QueueDraw();

			_contextMenu?.Destroy();
			_contextMenu = CreateContextMenu(group);
		});
	}

	private void ShowContextMenu(object sender, ContextMenuEventArgs args)
	{
		_contextMenu.Popup();
	}

	private MenuItem CreateMenuItem(string label, string iconName, Action action)
	{
		var box = new Box(Orientation.Horizontal, 0);

		if (!string.IsNullOrEmpty(iconName))
		{
			box.PackStart(Image.NewFromIconName(iconName, IconSize.Menu), false, false, 0);
		}

		box.PackStart(new Label(label), false, false, 0);
		var menuItem = new MenuItem();
		menuItem.Add(box);
		return menuItem;
	}

	private Menu CreateContextMenu(IconGroupViewModel group)
	{
		var contextMenu = new Menu();
		contextMenu.Add(CreateMenuItem("Pin to Dock", "", () => { }));
		contextMenu.Add(new SeparatorMenuItem());

		var allowedActions = group.Tasks.First().AllowedActions;
		var desktopFile = group.Tasks.First().DesktopFile;

		foreach (var action in _displayableActions)
		{
			var menuItem = CreateMenuItem(action.ToString(), "", () => { });
			menuItem.Sensitive = allowedActions.Contains(action);
			contextMenu.Add(menuItem);
		}

		if (desktopFile != null && desktopFile.Actions.Count > 0)
		{
			contextMenu.Add(new SeparatorMenuItem());

			foreach (var action in desktopFile.Actions)
			{
				contextMenu.Add(CreateMenuItem(action.ActionName, action.IconName, () => { }));
			}
		}

		if (allowedActions.Contains(AllowedWindowActions.Close))
		{
			contextMenu.Add(new SeparatorMenuItem());
			contextMenu.Add(CreateMenuItem("Close", "", () => { }));
		}

		contextMenu.ShowAll();
		return contextMenu;
	}

	protected override bool OnDrawn(Context cr)
	{
		cr.Save();

		var w = Window.Width;
		var h = Window.Height;

		if (_currentViewModel.Tasks.Count > 1)
		{
			var imageSurface = new ImageSurface(Format.ARGB32, w, h);
			var imageContext = new Context(imageSurface);

			imageContext.Operator = Operator.Over;
			imageContext.SetSourceRGBA(1, 0, 0, 1);
			imageContext.RoundedRectangle(0, 0, w-5, h, 4);
			imageContext.Fill();

			imageContext.Operator = Operator.Out;
			imageContext.SetSourceRGBA(0, 1, 0, 1);
			imageContext.RoundedRectangle(0, 0, w-2, h, 4);
			imageContext.Fill();

			imageContext.Operator = Operator.Out;
			imageContext.SetSourceRGBA(1, 1, 1, StateFlags.HasFlag(StateFlags.Prelight) ? 0.3 : 0.1);
			imageContext.RoundedRectangle(0, 0, w, h, 4);
			imageContext.Fill();

			cr.SetSourceSurface(imageSurface, 0, 0);
			cr.Paint();

			imageSurface.Dispose();
			imageContext.Dispose();
		}
		else
		{
			cr.Operator = Operator.Over;
			cr.SetSourceRGBA(1, 1, 1, StateFlags.HasFlag(StateFlags.Prelight) ? 0.3 : 0.1);
			cr.RoundedRectangle(0, 0, w, h, 4);
			cr.Fill();
		}

		cr.Restore();
		return base.OnDrawn(cr);
	}
}
