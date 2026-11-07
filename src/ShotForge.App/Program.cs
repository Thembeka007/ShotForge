using System;
using System.IO;
using Avalonia;
using ShotForge.App.Services;
using Velopack;

namespace ShotForge.App
{
    internal static class Program
    {
        // Avalonia entry point. Kept minimal on purpose -
        // all real startup logic lives in App.axaml.cs.
        [System.STAThread]
        public static void Main(string[] args)
        {
            // MUST be the very first line of Main, before anything else -
            // this is a hard Velopack requirement, not a style choice.
            // When the installer runs install/uninstall/update hooks, it
            // launches this exe with special arguments that this call
            // intercepts; if any other startup code runs first, those
            // hooks can fire at the wrong time or not at all. Harmless
            // (does nothing) on a normal app launch outside the installer.
            VelopackApp.Build().Run();

            // Developer-only switches (e.g. --force-pro). Does nothing in Release builds.
            DevOptions.Initialize(args);

            // Without this, an unhandled exception on the UI thread kills
            // the whole process with no readable trace once the console
            // window closes. This writes it to a local file first.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                LogCrash(e.ExceptionObject as Exception);

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void LogCrash(Exception? ex)
        {
            try
            {
                var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var shotForgeFolder = Path.Combine(appDataFolder, "ShotForge");
                Directory.CreateDirectory(shotForgeFolder);
                var logPath = Path.Combine(shotForgeFolder, "crash.log");
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}");
            }
            catch
            {
                // If logging itself fails, there's nothing more we can do here.
            }
        }
    }
}
