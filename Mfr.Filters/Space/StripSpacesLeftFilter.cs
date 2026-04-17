using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Removes any space characters from the beginning of text.
    /// </summary>
    /// <remarks>
    /// The space character is <c>U+0020 SPACE</c> by default, but can be changed by
    /// a preceding <c>SpaceCharacter</c> filter in the applied filters list.
    /// </remarks>
    /// <param name="Enabled">Whether the filter is enabled.</param>
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record StripSpacesLeftFilter(
        bool Enabled,
        FilterTarget Target) : BaseFilter(Enabled, Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "StripSpacesLeft";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return segment;
            }

            return segment.TrimStart(item.WordSeparator);
        }
    }
}
