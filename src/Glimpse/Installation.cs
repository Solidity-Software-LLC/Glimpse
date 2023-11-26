using System.Diagnostics;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;

namespace Glimpse;

public class Installation
{
	private const string InstallScriptResourceName = "install.sh";
	private const string InstallScriptTmpPath = "/tmp/glimpse-install.sh";

	public static void Install()
	{
		try
		{
			using var resourceStream = typeof(Installation).Assembly.GetManifestResourceStream(InstallScriptResourceName);
			using var reader = new StreamReader(resourceStream);
			var installScript = reader.ReadToEnd();

			File.WriteAllText(InstallScriptTmpPath, installScript);
			Process.Start("/bin/bash", $"-c \"chmod +x {InstallScriptTmpPath}\"")?.WaitForExit();

			using var p = new Process();
			p.EnableRaisingEvents = true;
			p.StartInfo = new ProcessStartInfo { FileName = "/bin/bash", UseShellExecute = false, Arguments = $"-c \"{InstallScriptTmpPath}\"", };

			p.Events().OutputDataReceived.TakeUntil(p.Events().Exited.Take(1)).Subscribe(a => Console.Write(a.Data));
			p.Start();
			p.WaitForExit();
		}
		finally
		{
			if (File.Exists(InstallScriptTmpPath))
			{
				File.Delete(InstallScriptTmpPath);
			}
		}
	}
}
