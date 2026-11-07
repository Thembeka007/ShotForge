using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ShotForge.Core.Models;

namespace ShotForge.Core.Services
{
    /// <summary>
    /// Turns a fully-loaded Project (with its Scenes and each Scene's
    /// Shots) into either a plug-and-play .txt file (the same format
    /// used throughout this project's own hand-written shot lists) or a
    /// structured .json file for re-import, backup, or scripting against
    /// an AI video API directly.
    /// </summary>
    public static class ProjectExportService
    {
        public static string BuildProjectText(Project project, PromptOptions? options = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine(project.Name.ToUpperInvariant());
            sb.AppendLine(new string('=', project.Name.Length));
            sb.AppendLine();

            var orderedScenes = project.Scenes.OrderBy(s => s.SceneNumber);

            foreach (var scene in orderedScenes)
            {
                sb.AppendLine($"SCENE {scene.SceneNumber} — {scene.Title}");
                sb.AppendLine(new string('-', 50));
                sb.AppendLine();

                var orderedShots = scene.Shots.OrderBy(sh => sh.ShotNumber);

                if (!orderedShots.Any())
                {
                    sb.AppendLine("(no shots added to this scene yet)");
                    sb.AppendLine();
                }

                foreach (var shot in orderedShots)
                {
                    sb.AppendLine($"Shot {shot.ShotNumber} ({shot.DurationSeconds}s):");
                    sb.AppendLine(shot.BuildPromptText(options));

                    // When the negative prompt is NOT part of the main prompt, still list it on
                    // its own line (same plain label) so nothing is lost from the export.
                    if (!(options ?? PromptOptions.Default).IncludeNegativeInPrompt
                        && !string.IsNullOrWhiteSpace(shot.NegativePrompt))
                    {
                        sb.AppendLine($"Negative prompt: {shot.NegativePrompt}");
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Structured export. Deliberately NOT serializing the EF
        /// entities directly - Scene.Project and Shot.Scene are
        /// back-references that would cause an infinite cycle, and the
        /// raw entities also carry internal fields (Ids, FK columns)
        /// that aren't useful to whatever reads this file. This shape is
        /// export-only, built specifically for re-import/scripting use.
        /// </summary>
        public static string BuildProjectJson(Project project, PromptOptions? options = null)
        {
            var exported = new ExportedProject
            {
                Name = project.Name,
                Scenes = project.Scenes
                    .OrderBy(s => s.SceneNumber)
                    .Select(scene => new ExportedScene
                    {
                        SceneNumber = scene.SceneNumber,
                        Title = scene.Title,
                        Shots = scene.Shots
                            .OrderBy(sh => sh.ShotNumber)
                            .Select(shot => new ExportedShot
                            {
                                ShotNumber = shot.ShotNumber,
                                DurationSeconds = shot.DurationSeconds,
                                Description = shot.Description,
                                NegativePrompt = shot.NegativePrompt,
                                StyleLockTag = shot.StyleLockTag,
                                FullPrompt = shot.BuildPromptText(options)
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return JsonSerializer.Serialize(exported, JsonOptions);
        }

        /// <summary>
        /// A safe default file name for the Save dialog - strips
        /// characters that aren't valid in file names on Windows/Mac.
        /// Extension is added by the caller based on the format chosen.
        /// </summary>
        public static string SuggestedFileNameBase(Project project)
        {
            return new string(project.Name
                .Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)
                .ToArray());
        }

        // --- Export-only DTOs: intentionally separate from the EF models ---

        private class ExportedProject
        {
            public string Name { get; set; } = string.Empty;
            public List<ExportedScene> Scenes { get; set; } = new();
        }

        private class ExportedScene
        {
            public int SceneNumber { get; set; }
            public string Title { get; set; } = string.Empty;
            public List<ExportedShot> Shots { get; set; } = new();
        }

        private class ExportedShot
        {
            public int ShotNumber { get; set; }
            public int DurationSeconds { get; set; }
            public string Description { get; set; } = string.Empty;
            public string? NegativePrompt { get; set; }
            public string StyleLockTag { get; set; } = string.Empty;
            public string FullPrompt { get; set; } = string.Empty;
        }
    }
}
