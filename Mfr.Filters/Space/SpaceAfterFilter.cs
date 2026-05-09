using System.Text;
using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Options for <see cref="SpaceAfterFilter"/>.
    /// </summary>
    /// <param name="AfterChars">Characters after which a word separator may be inserted.</param>
    /// <param name="OnlyWhenNextIsLetterOrDigit">
    /// When <c>true</c>, inserts only when the character immediately after the trigger is a Unicode letter or digit.
    /// </param>
    public sealed record SpaceAfterOptions(
        string AfterChars = "",
        bool OnlyWhenNextIsLetterOrDigit = false);

    /// <summary>
    /// Ensures the current word separator appears after each listed character when missing.
    /// <para>
    /// The inserted character is <see cref="RenameItem.WordSeparator"/> (U+0020 SPACE by default); a preceding
    /// <c>SpaceCharacter</c> filter sets that separator for this pass.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trigger characters and conditional insertion.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    public sealed record SpaceAfterFilter(
        FilterTarget Target,
        SpaceAfterOptions Options, StringApplyScope? ApplyScope = null) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SpaceAfter";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(Options.AfterChars))
                return value;

            var triggers = new HashSet<char>(Options.AfterChars);
            var sep = item.WordSeparator;
            var onlyWhenNext = Options.OnlyWhenNextIsLetterOrDigit;
            var builder = new StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                builder.Append(c);
                if (!triggers.Contains(c))
                    continue;
                if (i + 1 >= value.Length)
                    continue;

                var next = value[i + 1];
                if (SpaceTriggerInsertion.ShouldInsertBeside(next, sep, onlyWhenNext))
                    builder.Append(sep);
            }

            return builder.ToString();
        }
    }
}
