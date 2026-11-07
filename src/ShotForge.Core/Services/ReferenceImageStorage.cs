using System;
using System.IO;

namespace ShotForge.Core.Services
{
    /// <summary>
    /// Reference images are copied into ShotForge's own local folder
    /// rather than referenced at their original picked location - if the
    /// user moves or deletes the original file later, the app's copy is
    /// unaffected. This is a pure file-IO helper with no UI dependency,
    /// so it lives in Core rather than the App project.
    /// </summary>
    public static class ReferenceImageStorage
    {
        public static string GetReferencesFolder()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "ShotForge", "references");
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Copies the given source file into ShotForge's references
        /// folder under a new unique name. Returns the new path - this
        /// is what gets saved as Character.ReferenceImagePath.
        /// </summary>
        public static string SaveCopy(string sourceFilePath)
        {
            var extension = Path.GetExtension(sourceFilePath);
            var newFileName = $"{Guid.NewGuid()}{extension}";
            var destinationPath = Path.Combine(GetReferencesFolder(), newFileName);
            File.Copy(sourceFilePath, destinationPath, overwrite: false);
            return destinationPath;
        }

        /// <summary>
        /// Best-effort cleanup when a Character is deleted. Failures are
        /// swallowed deliberately - a leftover orphaned image file is a
        /// minor annoyance, not worth crashing a delete over.
        /// </summary>
        public static void DeleteIfExists(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best effort only.
            }
        }
    }
}
