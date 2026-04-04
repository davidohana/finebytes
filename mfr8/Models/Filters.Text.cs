namespace Mfr8.Models
{
    /// <summary>
    /// Options for letter-case transformations.
    /// </summary>
    /// <param name="Mode">Case transformation mode.</param>
    /// <param name="SkipWords">Words to leave lowercased in title case mode.</param>
    public sealed record LettersCaseOptions(
        LettersCaseMode Mode,
        IReadOnlyList<string> SkipWords);

    /// <summary>
    /// Supported letter-case transformation modes.
    /// </summary>
    public enum LettersCaseMode
    {
        UpperCase,
        LowerCase,
        TitleCase,
        SentenceCase,
        InvertCase
    }

    /// <summary>
    /// Converts text letter casing.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Case transformation options.</param>
    public sealed record LettersCaseFilter(
        bool Enabled,
        FilterTarget Target,
        LettersCaseOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "LettersCase";
    }

    /// <summary>
    /// Options for replacing spaces and replacing a chosen character with spaces.
    /// </summary>
    /// <param name="ReplaceSpaceWith">Replacement text for spaces.</param>
    /// <param name="ReplaceCharWithSpace">Character/text to replace with a space.</param>
    public sealed record SpaceCharacterOptions(
        string ReplaceSpaceWith,
        string ReplaceCharWithSpace);

    /// <summary>
    /// Replaces spaces and mapped characters.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Space replacement options.</param>
    public sealed record SpaceCharacterFilter(
        bool Enabled,
        FilterTarget Target,
        SpaceCharacterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SpaceCharacter";
    }

    /// <summary>
    /// Removes all whitespace from the target segment.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record RemoveSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "RemoveSpaces";
    }

    /// <summary>
    /// Collapses runs of whitespace into single spaces.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record ShrinkSpacesFilter(
        bool Enabled,
        FilterTarget Target) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ShrinkSpaces";
    }

    /// <summary>
    /// Trims a fixed number of characters from the left.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trim options.</param>
    public sealed record TrimLeftFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimLeft";
    }

    /// <summary>
    /// Trims a fixed number of characters from the right.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trim options.</param>
    public sealed record TrimRightFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimRight";
    }

    /// <summary>
    /// Extracts a fixed number of characters from the left.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    public sealed record ExtractLeftFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractLeft";
    }

    /// <summary>
    /// Extracts a fixed number of characters from the right.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Extraction options.</param>
    public sealed record ExtractRightFilter(
        bool Enabled,
        FilterTarget Target,
        CountFilterOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "ExtractRight";
    }
}
