using Avalonia.Media;
using ShotForge.Core.Models;

namespace ShotForge.App.ViewModels
{
    /// <summary>
    /// Pairs a Character with its loaded thumbnail for display in the
    /// Characters panel. Kept separate from the Character model itself
    /// since IImage is an Avalonia UI type - the Core project has no
    /// Avalonia dependency, and shouldn't need one just to hold a
    /// picture reference.
    /// </summary>
    public class CharacterListItem
    {
        public Character Character { get; }
        public IImage? Thumbnail { get; }

        public CharacterListItem(Character character, IImage? thumbnail)
        {
            Character = character;
            Thumbnail = thumbnail;
        }

        public string Name => Character.Name;
        public string StandingDescription => Character.StandingDescription;
    }
}
