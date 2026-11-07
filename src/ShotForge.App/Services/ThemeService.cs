using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;

namespace ShotForge.App.Services
{
    /// <summary>
    /// Applies the Light/Dark theme and remembers the choice between
    /// launches in %AppData%\ShotForge\settings.json. Defaults to Dark
    /// (the brand look) when nothing has been saved yet.
    /// </summary>
    public static class ThemeService
    {
        private class AppSettings
        {
            public bool IsDark { get; set; } = true;
        }

        private static string SettingsPath
        {
            get
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShotForge");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "settings.json");
            }
        }

        public static bool LoadIsDark()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return true;
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                return settings?.IsDark ?? true;
            }
            catch
            {
                // A corrupt settings file should never stop the app starting.
                return true;
            }
        }

        public static void Apply(bool isDark)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }

        public static void Save(bool isDark)
        {
            try
            {
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new AppSettings { IsDark = isDark }));
            }
            catch
            {
                // Not being able to remember the choice is not worth an error.
            }
        }
    }
}
