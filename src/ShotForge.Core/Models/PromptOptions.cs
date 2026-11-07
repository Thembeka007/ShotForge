namespace ShotForge.Core.Models
{
    /// <summary>
    /// How a shot is turned into copy-ready text. Tools differ (some have a
    /// separate negative-prompt box, some have their own duration setting, some
    /// can't take reference images), so these are user-controlled settings
    /// rather than hard-coded behaviour.
    /// </summary>
    public record PromptOptions(
        bool IncludeCharacterDescriptions = true,
        bool IncludeNegativeInPrompt = false,
        bool IncludeDuration = true)
    {
        public static PromptOptions Default { get; } = new();
    }
}
