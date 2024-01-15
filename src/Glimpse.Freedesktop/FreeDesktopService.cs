using System.Collections.Immutable;
using System.Reactive.Linq;
using Glimpse.Freedesktop.DBus;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Redux;
using Task = System.Threading.Tasks.Task;

namespace Glimpse.Freedesktop.DesktopEntries;

public class FreeDesktopService(ReduxStore store, OrgFreedesktopAccounts freedesktopAccounts)
{
	private ImmutableList<DesktopFile> _desktopFiles;

	public async Task InitializeAsync(DBusConnections dBusConnections)
	{
		var userObjectPath = await freedesktopAccounts.FindUserByNameAsync(Environment.UserName);
		var userService = new OrgFreedesktopAccountsUser(dBusConnections.System, "org.freedesktop.Accounts", userObjectPath);

		Observable
			.Return(await userService.GetAllPropertiesAsync())
			.Concat(userService.PropertiesChanged)
			.Subscribe(p =>
			{
				store.Dispatch(new UpdateUserAction() { UserName = p.UserName, IconPath = p.IconFile });
			});
	}
}
