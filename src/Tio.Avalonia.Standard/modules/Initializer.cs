using Tio.Avalonia.Standard.Modules.DiskIO;
using Tio.Avalonia.Standard.Modules.Platform;

namespace Tio.Avalonia.Standard.Modules;

public class Initializer
{
    public static void Program(string appName)
    {
        Logger.Initialize(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName, "Logs"), appName);
        if (RunnerTypeDetector.IsDesktop) DesktopTypeDetector.Initialize();
    }
}