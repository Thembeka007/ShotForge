using System.Collections.Generic;

namespace ShotForge.Core.Models
{
    /// <summary>
    /// A scene within a project (e.g. "Scene 4 - The Hero Nobody Hired").
    /// </summary>
    public class Scene : ObservableModelBase
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        public int SceneNumber { get; set; }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // Navigation property
        public List<Shot> Shots { get; set; } = new();
    }
}
