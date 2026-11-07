using CommunityToolkit.Mvvm.ComponentModel;
using ShotForge.Core.Models;

namespace ShotForge.App.ViewModels
{
    /// <summary>
    /// Character itself has no "is this one picked for the current shot"
    /// concept - that's purely a UI-selection concern for the moment
    /// you're filling out the Shot form, not something that belongs on
    /// the Core model. This wrapper carries that state without touching
    /// Character.
    /// </summary>
    public partial class CharacterSelectionItem : ObservableObject
    {
        public Character Character { get; }

        [ObservableProperty]
        private bool _isSelected;

        public CharacterSelectionItem(Character character)
        {
            Character = character;
        }

        public string Name => Character.Name;
    }
}
