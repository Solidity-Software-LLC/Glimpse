using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive.Linq;
using GLib;
using Glimpse.Common.System.Runtime.InteropServices;
using Glimpse.Freedesktop.DBus;
using Glimpse.Freedesktop.DBus.Interfaces;
using Glimpse.Interop.Gdk;
using Glimpse.Redux;
using ReactiveMarbles.ObservableEvents;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace Glimpse.Freedesktop.DesktopEntries;

public class FreeDesktopService(ReduxStore store, OrgFreedesktopAccounts freedesktopAccounts)
{
	private ImmutableList<DesktopFile> _desktopFiles;

	public async Task InitializeAsync(DBusConnections dBusConnections)
	{
		await LoadDesktopFiles();
		AppInfoMonitor.Get().Events().Changed.Subscribe(_ => LoadDesktopFiles());

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

	private async Task LoadDesktopFiles()
	{
		_desktopFiles = AppInfoAdapter.GetAll().Where(a => a.ShouldShow).Select(CreateDesktopFile).ToImmutableList();
		await store.Dispatch(new UpdateDesktopFilesAction() { DesktopFiles = _desktopFiles });
	}

	public void Run(DesktopFile desktopFile)
	{
		var startInfo = new ProcessStartInfo("setsid", "xdg-open " + desktopFile.FilePath);
		startInfo.WorkingDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		Process.Start(startInfo);
	}

	public void Run(DesktopFileAction action)
	{
		var gDesktopFile = LibGdk3Interop.g_desktop_app_info_new_from_filename(action.DesktopFilePath);
		LibGdk3Interop.g_desktop_app_info_launch_action(gDesktopFile, action.Id, IntPtr.Zero);
	}

	public void Run(string command)
	{
		var startInfo = new ProcessStartInfo("setsid", command);
		startInfo.WorkingDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
		Process.Start(startInfo);
	}

	private static DesktopFile CreateDesktopFile(IAppInfo a)
	{
		var filePath = LibGdk3Interop.g_desktop_app_info_get_filename(a.Handle);
		var actionNames = MarshalExtensions.PtrToStringArray(LibGdk3Interop.g_desktop_app_info_list_actions(a.Handle));

		var actions = actionNames
			.Select(actionId =>
			{
				var actionIdPtr = Marshaller.StringToPtrGStrdup(actionId);
				var actionNamePtr = LibGdk3Interop.g_desktop_app_info_get_action_name(a.Handle, actionIdPtr);
				var actionName = Marshaller.PtrToStringGFree(actionNamePtr);
				Marshaller.Free(actionIdPtr);
				return new DesktopFileAction() { Id = actionId, ActionName = actionName, DesktopFilePath = filePath };
			})
			.ToList();

		var desktopFile = new DesktopFile()
		{
			Id = filePath,
			FileName = Path.GetFileNameWithoutExtension(filePath),
			Name = a.Name,
			IconName = LibGdk3Interop.g_desktop_app_info_get_string(a.Handle, "Icon") ?? "",
			Executable = a.Executable,
			CommandLine = a.Commandline,
			StartupWmClass = LibGdk3Interop.g_desktop_app_info_get_startup_wm_class(a.Handle) ?? "",
			Actions = actions,
			Categories = ParseCategories(LibGdk3Interop.g_desktop_app_info_get_categories(a.Handle))
		};

		return desktopFile;
	}

	private static List<string> ParseCategories(string categories)
	{
		if (string.IsNullOrEmpty(categories)) return null;
		return categories.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList();
	}
}
