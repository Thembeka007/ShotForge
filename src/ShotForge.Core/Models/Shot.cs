using System.Collections.Generic;
using System.Linq;

namespace ShotForge.Core.Models
{
    public enum ShotStatus
    {
        Pending,
        Generated,
        Approved,
        NeedsRegeneration
    }

    /// <summary>
    /// A single generation-ready shot: one visual beat, one prompt,
    /// one duration, tied to whichever characters appear in it.
    /// </summary>
    public class Shot : ObservableModelBase
    {
        public int Id { get; set; }
        public int SceneId { get; set; }
        public Scene? Scene { get; set; }

        public int ShotNumber { get; set; }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string? _negativePrompt;
        public string? NegativePrompt
        {
            get => _negativePrompt;
            set => SetProperty(ref _negativePrompt, value);
        }

        private int _durationSeconds = 5;
        public int DurationSeconds
        {
            get => _durationSeconds;
            set => SetProperty(ref _durationSeconds, value);
        }

        private string _styleLockTag = string.Empty;
        public string StyleLockTag
        {
            get => _styleLockTag;
            set => SetProperty(ref _styleLockTag, value);
        }

        public ShotStatus Status { get; set; } = ShotStatus.Pending;

        private List<Character> _characters = new();

        // Many-to-many navigation (a shot can feature many characters).
        // Notifies CharacterNamesDisplay/HasCharacters too, since both
        // are computed from this and SetProperty alone only notifies
        // "Characters" itself.
        public List<Character> Characters
        {
            get => _characters;
            set
            {
                if (SetProperty(ref _characters, value))
                {
                    OnPropertyChanged(nameof(CharacterNamesDisplay));
                    OnPropertyChanged(nameof(HasCharacters));
                }
            }
        }

        /// <summary>
        /// Comma-joined character names for display in the shot list
        /// (e.g. "Kai, Bolt"). A plain computed getter, same layering
        /// choice already made for BuildPromptText() below - Avalonia's
        /// binding syntax has no inline way to join a collection into a
        /// string, so this small piece of display logic lives here
        /// rather than being reimplemented in the View.
        /// </summary>
        public string CharacterNamesDisplay =>
            Characters.Count == 0 ? string.Empty : string.Join(", ", Characters.Select(c => c.Name));

        public bool HasCharacters => Characters.Count > 0;

        /// <summary>
        /// Builds the copy-ready prompt: up to five parts separated by blank lines -
        ///   1. the description (what happens)
        ///   2. "Duration: N seconds"                  (if enabled)
        ///   3. "Characters: Name - description; ..."  (if enabled and any are tagged)
        ///   4. "Negative prompt: ..."                  (only if the negative prompt is
        ///                                              set to go INSIDE the prompt)
        ///   5. the style line                         (if one was entered)
        /// By default the negative prompt is NOT included - tools that have their own
        /// negative box want it pasted there; it stays visible on the shot card and in exports.
        /// No "Shot N" labels: those only mean something to multi-shot batch modes.
        /// </summary>
        public string BuildPromptText(PromptOptions? options = null)
        {
            options ??= PromptOptions.Default;
            var lines = new List<string> { Description };

            if (options.IncludeDuration && DurationSeconds > 0)
            {
                lines.Add(DurationSeconds == 1 ? "Duration: 1 second" : $"Duration: {DurationSeconds} seconds");
            }

            if (options.IncludeCharacterDescriptions && Characters.Count > 0)
            {
                var parts = Characters.Select(c =>
                    string.IsNullOrWhiteSpace(c.StandingDescription)
                        ? c.Name
                        : $"{c.Name} - {c.StandingDescription.Trim()}");
                lines.Add("Characters: " + string.Join("; ", parts));
            }

            if (options.IncludeNegativeInPrompt && !string.IsNullOrWhiteSpace(NegativePrompt))
            {
                lines.Add($"Negative prompt: {NegativePrompt}");
            }

            if (!string.IsNullOrWhiteSpace(StyleLockTag))
            {
                lines.Add(StyleLockTag);
            }

            return string.Join("\n\n", lines);
        }
    }
}
