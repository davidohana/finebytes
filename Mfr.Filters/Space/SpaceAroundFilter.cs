using System.Text;
using Mfr.Models;

namespace Mfr.Filters.Space
{
    /// <summary>
    /// Options for <see cref="SpaceAroundFilter"/>.
    /// </summary>
    /// <param name="AroundChars">Characters before and after which a word separator may be inserted.</param>
    /// <param name="OnlyWhenNeighboringAreLettersOrDigits">
    /// When <c>true</c>, inserts only when the neighbor on that side (before or after the trigger) is a Unicode letter or digit.
    /// </param>
    public sealed record SpaceAroundOptions(
        string AroundChars = "",
        bool OnlyWhenNeighboringAreLettersOrDigits = false);

    /// <summary>
    /// Ensures the current word separator appears before and after each listed character when missing.
    /// <para>
    /// The inserted character is <see cref="RenameItem.WordSeparator"/> (U+0020 SPACE by default); a preceding
    /// <c>SpaceCharacter</c> filter sets that separator for this pass.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trigger characters and conditional insertion.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    public sealed record SpaceAroundFilter(
        FilterTarget Target,
        SpaceAroundOptions Options, StringApplyScope? ApplyScope = null) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "SpaceAround";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(Options.AroundChars))
                return value;

            var triggers = new HashSet<char>(Options.AroundChars);
            var sep = item.WordSeparator;
            var onlyWhenNeighbor = Options.OnlyWhenNeighboringAreLettersOrDigits;
            var builder = new StringBuilder(value.Length + 16);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (!triggers.Contains(c))
                {
                    builder.Append(c);
                    continue;
                }

                if (i > 0)
                {
                    var prev = value[i - 1];
                    if (SpaceTriggerInsertion.ShouldInsertBeside(prev, sep, onlyWhenNeighbor))
                        builder.Append(sep);
                }

                builder.Append(c);

                if (i + 1 < value.Length)
                {
                    var next = value[i + 1];
                    if (SpaceTriggerInsertion.ShouldInsertBeside(next, sep, onlyWhenNeighbor))
                        builder.Append(sep);
                }
            }

            return builder.ToString();
        }
    }
}
