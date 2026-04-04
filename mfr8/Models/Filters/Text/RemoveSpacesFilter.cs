namespace Mfr8.Models
{
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

        internal override string Apply(string segment, FileEntryLite file)
        {
            return TextFilterRegexCache.WhitespaceRegex.Replace(segment, "");
        }
    }
}
