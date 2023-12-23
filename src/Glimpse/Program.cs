using System.CommandLine;
using System.Reactive.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Glimpse.Configuration;
using Glimpse.Freedesktop;
using Glimpse.Freedesktop.DBus;
using Glimpse.Redux;
using Glimpse.Redux.Effects;
using Glimpse.UI;
using Glimpse.Xorg;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Glimpse;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		var installCommand = new Command("install", "Install Glimpse");
		installCommand.AddAlias("i");
		installCommand.SetHandler(async _ =>
		{
			Installation.RunScript(Installation.InstallScriptResourceName);

			var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
			builder.ConfigureContainer(new AutofacServiceProviderFactory(ConfigureContainer));
			var host = builder.Build();

			var connections = host.Services.GetRequiredService<DBusConnections>();
			await connections.System.ConnectAsync();
			await connections.Session.ConnectAsync();

			var xSessionManager = host.Services.GetRequiredService<XSessionManager>();
			await xSessionManager.Register(Installation.DefaultInstallPath);
		});

		var uninstallCommand = new Command("uninstall", "Uninstall Glimpse");
		uninstallCommand.AddAlias("u");
		uninstallCommand.SetHandler(_ => Installation.RunScript(Installation.UninstallScriptResourceName));

		var rootCommand = new RootCommand("Glimpse");
		rootCommand.AddCommand(installCommand);
		rootCommand.AddCommand(uninstallCommand);
		rootCommand.SetHandler(async c => c.ExitCode = await RunGlimpseAsync());

		return await rootCommand.InvokeAsync(args);
	}

	private static async Task<int> RunGlimpseAsync()
	{
		try
		{
			AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject);
			var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

			builder.ConfigureContainer(new AutofacServiceProviderFactory(cb =>
			{
				ConfigureContainer(cb);
				cb.AddGlimpseConfiguration();
				cb.AddFreedesktop();
				cb.AddXorg();
				cb.AddGlimpseUI();
			}));

			builder.AddXorg();
			builder.AddGlimpseUI();

			var host = builder.Build();
			await host.UseGlimpseConfiguration();
			await host.UseFreedesktop(Installation.DefaultInstallPath);
			await host.UseXorg();
			await host.UseGlimpseUI();

			var store = host.Services.GetRequiredService<ReduxStore>();
			var effects = host.Services.GetRequiredService<IEffectsFactory[]>()
				.SelectMany(e => e.Create())
				.Select(oldEffect => new Effect()
				{
					Run = _ => oldEffect.Run(store).Do(_ => { }, exception => Console.WriteLine(exception)),
					Config = oldEffect.Config
				})
				.ToArray();

			store.RegisterEffects(effects);
			await store.Dispatch(new InitializeStoreAction());
			await host.RunAsync();
			return 0;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return 1;
		}
	}

	private static void ConfigureContainer(ContainerBuilder containerBuilder)
	{
		containerBuilder.RegisterType<ReduxStore>().SingleInstance();
	}
}
