using System;
using System.Collections.Generic;

namespace ShotForge.Core.Models
{
    /// <summary>
    /// A single video project (e.g. "ALIEN Attacks Humanity - S1E1").
    /// </summary>
    public class Project : ObservableModelBase
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public List<Scene> Scenes { get; set; } = new();
        public List<Character> Characters { get; set; } = new();
    }
}
