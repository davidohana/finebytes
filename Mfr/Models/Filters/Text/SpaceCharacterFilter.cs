namespace Mfr.Models.Filters.Text
{
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

        internal override string ApplySegment(string segment, RenameItem item)
        {
            return segment.Replace(" ", Options.ReplaceSpaceWith).Replace(Options.ReplaceCharWithSpace, " ");
        }
    }
}
