namespace Mfr.Models.Filters.Text
{
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

        internal override string Apply(string segment, RenameItem item)
        {
            return Options.Count <= 0 ? segment : segment.Length <= Options.Count ? "" : segment[Options.Count..];
        }
    }
}
