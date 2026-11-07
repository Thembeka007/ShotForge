using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShotForge.Core.Services;
using ShotForge.Data.Repositories;

namespace ShotForge.App.ViewModels
{
    public partial class CharactersWindowViewModel : ObservableObject
    {
        private readonly CharacterRepository _characterRepository;
        private readonly int _projectId;

        [ObservableProperty]
        private ObservableCollection<CharacterListItem> _characters = new();

        /// <summary>
        /// NotifyCanExecuteChangedFor is what makes AddCharacterCommand
        /// re-check its enabled state on every keystroke here - without
        /// it, the button computes its enabled state once and never
        /// re-evaluates, staying disabled even after a name is typed.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddCharacterCommand))]
        private string _newCharacterName = string.Empty;

        [ObservableProperty]
        private string _newCharacterDescription = string.Empty;

        /// <summary>
        /// The file actually picked via the file dialog, held here until
        /// "Add Character" is clicked. Null means no image chosen yet -
        /// that's allowed, since a character can be added text-only and
        /// given an image later via Update (not yet built).
        /// </summary>
        [ObservableProperty]
        private string? _pendingImagePath;

        [ObservableProperty]
        private IImage? _pendingImagePreview;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddOrUpdateCharacterButtonText))]
        private bool _isEditingCharacter;

        private CharacterListItem? _editingItem;

        public string AddOrUpdateCharacterButtonText => IsEditingCharacter ? "Update Character" : "Add Character";

        public string ProjectDisplayName { get; }

        public CharactersWindowViewModel(
            CharacterRepository characterRepository, int projectId, string projectName)
        {
            _characterRepository = characterRepository;
            _projectId = projectId;
            ProjectDisplayName = projectName;

            _ = LoadCharactersAsync();
        }

        private async Task LoadCharactersAsync()
        {
            var characters = await _characterRepository.GetByProjectIdAsync(_projectId);
            var items = new ObservableCollection<CharacterListItem>();

            foreach (var character in characters)
            {
                items.Add(new CharacterListItem(character, TryLoadThumbnail(character.ReferenceImagePath)));
            }

            Characters = items;
        }

        /// <summary>
        /// Called from the code-behind after the user picks a file in
        /// the Open-file dialog - a platform action that has to live in
        /// the View, same split used for the clipboard/export features.
        /// </summary>
        public void SetPendingImage(string filePath)
        {
            PendingImagePath = filePath;
            PendingImagePreview = TryLoadThumbnail(filePath);
        }

        private bool CanAddCharacter() => !string.IsNullOrWhiteSpace(NewCharacterName);

        [RelayCommand(CanExecute = nameof(CanAddCharacter))]
        private async Task AddCharacterAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCharacterName))
                return;

            if (_editingItem is not null)
            {
                var character = _editingItem.Character;

                // Only touch the stored image file if the user actually
                // picked a DIFFERENT one during this edit. Comparing
                // against character.ReferenceImagePath here is safe
                // because we haven't reassigned it yet at this point -
                // it still holds whatever was there before this edit
                // started. Without this check, saving an edit where the
                // image wasn't touched would silently copy the already-
                // stored file again under a new name every single time.
                var imageWasChanged = !string.Equals(
                    PendingImagePath, character.ReferenceImagePath, StringComparison.Ordinal);

                var finalImagePath = character.ReferenceImagePath;
                if (imageWasChanged)
                {
                    var oldPath = character.ReferenceImagePath;
                    finalImagePath = string.IsNullOrWhiteSpace(PendingImagePath)
                        ? null
                        : ReferenceImageStorage.SaveCopy(PendingImagePath!);
                    ReferenceImageStorage.DeleteIfExists(oldPath);
                }

                character.Name = NewCharacterName;
                character.StandingDescription = NewCharacterDescription;
                character.ReferenceImagePath = finalImagePath;
                await _characterRepository.UpdateAsync(character);

                // CharacterListItem's Thumbnail is set once at construction
                // (not itself observable), so if the image changed, the
                // roster needs a freshly-built item to show it - replacing
                // by index is simpler than making Thumbnail observable
                // just for this one case.
                var index = Characters.IndexOf(_editingItem);
                if (index >= 0)
                {
                    Characters[index] = new CharacterListItem(character, TryLoadThumbnail(finalImagePath));
                }

                _editingItem = null;
                IsEditingCharacter = false;
            }
            else
            {
                // Copy the picked image into ShotForge's own storage now,
                // at the moment the character is actually saved - not
                // earlier, so cancelling the form doesn't leave an orphaned
                // copy behind.
                string? savedImagePath = null;
                if (!string.IsNullOrWhiteSpace(PendingImagePath))
                {
                    savedImagePath = ReferenceImageStorage.SaveCopy(PendingImagePath);
                }

                var created = await _characterRepository.CreateAsync(
                    _projectId, NewCharacterName, NewCharacterDescription, savedImagePath);

                Characters.Add(new CharacterListItem(created, TryLoadThumbnail(savedImagePath)));
            }

            NewCharacterName = string.Empty;
            NewCharacterDescription = string.Empty;
            PendingImagePath = null;
            PendingImagePreview = null;
        }

        [RelayCommand]
        private void EditCharacter(CharacterListItem? item)
        {
            if (item is null) return;

            _editingItem = item;
            NewCharacterName = item.Character.Name;
            NewCharacterDescription = item.Character.StandingDescription;
            // Pre-fill with the EXISTING stored path, not null - this is
            // what AddCharacterAsync compares against to detect whether
            // the user actually picked a new image during this edit.
            PendingImagePath = item.Character.ReferenceImagePath;
            PendingImagePreview = item.Thumbnail;
            IsEditingCharacter = true;
        }

        [RelayCommand]
        private void CancelEditCharacter()
        {
            _editingItem = null;
            NewCharacterName = string.Empty;
            NewCharacterDescription = string.Empty;
            PendingImagePath = null;
            PendingImagePreview = null;
            IsEditingCharacter = false;
        }

        [RelayCommand]
        private async Task DeleteCharacterAsync(CharacterListItem? item)
        {
            if (item is null) return;

            if (_editingItem == item)
            {
                CancelEditCharacter();
            }

            await _characterRepository.DeleteAsync(item.Character.Id);
            ReferenceImageStorage.DeleteIfExists(item.Character.ReferenceImagePath);
            Characters.Remove(item);
        }

        /// <summary>
        /// Loading a bitmap from disk can fail for all sorts of reasons
        /// (file moved, corrupted, unsupported format) - none of which
        /// should crash the whole panel. A missing thumbnail is a small
        /// visual gap, not a fatal error.
        /// </summary>
        private static IImage? TryLoadThumbnail(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            try
            {
                using var stream = File.OpenRead(path);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}
