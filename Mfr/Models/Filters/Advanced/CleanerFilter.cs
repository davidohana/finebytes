namespace Mfr8.Models.Filters.Advanced
{
    /// <summary>
    /// Options for illegal/custom character cleanup.
    /// </summary>
    /// <param name="RemoveIllegalChars">Whether illegal file-name characters are removed/replaced.</param>
    /// <param name="IllegalCharReplacement">Replacement value for illegal characters.</param>
    /// <param name="CustomCharsToRemove">Custom characters to remove/replace.</param>
    /// <param name="CustomReplacement">Replacement value for custom characters.</param>
    public sealed record CleanerOptions(
        bool RemoveIllegalChars,
        string IllegalCharReplacement,
        string CustomCharsToRemove,
        string CustomReplacement);

    /// <summary>
    /// Cleans illegal and custom characters.
    /// </summary>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Cleaner options.</param>
    public sealed record CleanerFilter(
        bool Enabled,
        FilterTarget Target,
        CleanerOptions Options) : Filter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Cleaner";

        internal override string Apply(string segment, FileEntryLite file)
        {
            var res = segment;
            if (Options.RemoveIllegalChars)
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    res = res.Replace(c.ToString(), Options.IllegalCharReplacement);
                }
            }

            if (!string.IsNullOrEmpty(Options.CustomCharsToRemove))
            {
                foreach (var c in Options.CustomCharsToRemove)
                {
                    res = res.Replace(c.ToString(), Options.CustomReplacement);
                }
            }

            return res;
        }
    }
}
