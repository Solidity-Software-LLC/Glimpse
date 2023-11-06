using System.Reactive.Linq;
using Gdk;
using Glimpse.Extensions.Gtk;
using Gtk;

namespace Glimpse.Components.StartMenu.Window;

public class StartMenuActionBar : Box
{
	public IObservable<string> CommandInvoked { get; private set; }

	public StartMenuActionBar(IObservable<ActionBarViewModel> viewModel)
		: base(Orientation.Horizontal, 0)
	{
		var userImage = new Image().AddClass("start-menu__account-icon");

		viewModel
			.Select(vm => vm.UserIconPath)
			.DistinctUntilChanged()
			.TakeUntilDestroyed(this)
			.Select(path => string.IsNullOrEmpty(path) || !File.Exists(path) ? Assets.Person.ScaleSimple(42, 42, InterpType.Bilinear) : new Pixbuf(path))
			.Select(p => p.ScaleSimple(42, 42, InterpType.Bilinear))
			.Subscribe(p => userImage.Pixbuf = p);

		var userButton = new Button()
			.AddClass("start-menu__user-settings-button").AddMany(
				new Box(Orientation.Horizontal, 0).AddMany(
					userImage,
					new Label(Environment.UserName).AddClass("start-menu__username")));

		userButton.Valign = Align.Center;

		var settingsButton = new Button(new Image(Assets.Settings.ScaleSimple(24, 24, InterpType.Bilinear)));
		settingsButton.AddClass("start-menu__settings");
		settingsButton.Valign = Align.Center;
		settingsButton.Halign = Align.End;

		var powerButton = new Button(new Image(Assets.Power.ScaleSimple(24, 24, InterpType.Bilinear)));
		powerButton.AddClass("start-menu__power");
		powerButton.Valign = Align.Center;
		powerButton.Halign = Align.End;

		Expand = true;
		this.AddClass("start-menu__action-bar");
		this.AddMany(userButton, new Label(Environment.MachineName) { Expand = true }, settingsButton, powerButton);

		CommandInvoked = userButton.ObserveButtonRelease().WithLatestFrom(viewModel).Select(t => t.Second.UserSettingsCommand)
			.Merge(powerButton.ObserveButtonRelease().WithLatestFrom(viewModel).Select(t => t.Second.PowerButtonCommand))
			.Merge(settingsButton.ObserveButtonRelease().WithLatestFrom(viewModel).Select(t => t.Second.SettingsButtonCommand));
	}
}
