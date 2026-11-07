using System.Collections.Generic;

namespace ShotForge.Core.Models
{
    /// <summary>
    /// A recurring character (or standing element, e.g. "the war-mech")
    /// with a saved reference image and a standing description used
    /// to keep every shot consistent.
    /// </summary>
    public class Character : ObservableModelBase
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _standingDescription = string.Empty;
        public string StandingDescription
        {
            get => _standingDescription;
            set => SetProperty(ref _standingDescription, value);
        }

        private string? _referenceImagePath;

        /// <summary>Absolute path to the saved reference image on disk.</summary>
        public string? ReferenceImagePath
        {
            get => _referenceImagePath;
            set => SetProperty(ref _referenceImagePath, value);
        }

        // Many-to-many navigation (a character can appear in many shots)
        public List<Shot> Shots { get; set; } = new();
    }
}
