using Mfr.Models;

namespace Mfr.Filters.Replace
{
    /// <summary>
    /// Options for illegal/custom character cleanup.
    /// </summary>
    /// <param name="RemoveIllegalChars">Whether illegal file-name characters are removed/replaced.</param>
    /// <param name="CustomCharsToRemove">Custom characters to remove/replace.</param>
    /// <param name="Replacement">Replacement value for both illegal and custom characters.</param>
    public sealed record CleanerOptions(
        bool RemoveIllegalChars,
        string CustomCharsToRemove,
        string Replacement);

    /// <summary>
    /// Cleans illegal and custom characters.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Cleaner options.</param>
    public sealed record CleanerFilter(
        FilterTarget Target,
        CleanerOptions Options) : BaseFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Cleaner";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            var invalidChars = Options.RemoveIllegalChars ? Path.GetInvalidFileNameChars() : [];
            var customChars = Options.CustomCharsToRemove ?? "";
            var chars = invalidChars.Concat(customChars).ToHashSet();

            if (chars.Count == 0)
            {
                return segment;
            }

            var sb = new System.Text.StringBuilder(segment.Length);
            foreach (var c in segment)
            {
                if (chars.Contains(c))
                {
                    sb.Append(Options.Replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
