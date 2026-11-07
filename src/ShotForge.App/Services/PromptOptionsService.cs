using System;
using System.IO;
using System.Text.Json;
using ShotForge.Core.Models;

namespace ShotForge.App.Services
{
    /// <summary>
    /// Remembers the "Copied prompt" checkboxes between launches in
    /// %AppData%\ShotForge\prompt-options.json.
    /// </summary>
    public static class PromptOptionsService
    {
        private class Stored
        {
            public bool IncludeCharacterDescriptions { get; set; } = true;
            public bool IncludeNegativeInPrompt { get; set; } = false;
            public bool IncludeDuration { get; set; } = true;
        }

        private static string FilePath
        {
            get
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShotForge");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "prompt-options.json");
            }
        }

        public static PromptOptions Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return PromptOptions.Default;
                var stored = JsonSerializer.Deserialize<Stored>(File.ReadAllText(FilePath)) ?? new Stored();
                return new PromptOptions(stored.IncludeCharacterDescriptions, stored.IncludeNegativeInPrompt, stored.IncludeDuration);
            }
            catch
            {
                return PromptOptions.Default;   // a damaged file must never stop the app starting
            }
        }

        public static void Save(PromptOptions options)
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(new Stored
                {
                    IncludeCharacterDescriptions = options.IncludeCharacterDescriptions,
                    IncludeNegativeInPrompt = options.IncludeNegativeInPrompt,
                    IncludeDuration = options.IncludeDuration
                }));
            }
            catch { /* not being able to remember a checkbox is not worth an error */ }
        }
    }
}
