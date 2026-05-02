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
    /// <param name="Target">The target that this filter applies to.</param>
    public sealed record StripSpacesLeftFilter(
        FilterTarget Target) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "StripSpacesLeft";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.TrimStart(item.WordSeparator);
        }
    }
}
