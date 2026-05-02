using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Options for moving a single delimiter-separated token within a segment.
    /// </summary>
    /// <param name="Delimiter">Substring that separates tokens; must be non-empty for the filter to split the segment.</param>
    /// <param name="TokenNumber">One-based index of the token to move (first token is <c>1</c>).</param>
    /// <param name="MoveBy">Offset in token positions: positive toward the end, negative toward the start.</param>
    public sealed record TokenMoverOptions(
        string Delimiter,
        int TokenNumber,
        int MoveBy);

    /// <summary>
    /// Moves one delimiter-separated token within the segment.
    /// <para>
    /// Splits by <see cref="TokenMoverOptions.Delimiter"/>, moves the selected token by <see cref="TokenMoverOptions.MoveBy"/>,
    /// clamping to the first or last slot when the offset would leave the token list.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Token mover options.</param>
    public sealed record TokenMoverFilter(
        FilterTarget Target,
        TokenMoverOptions Options) : StringTargetFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TokenMover";

        /// <inheritdoc />
        protected override string _TransformValue(string value, RenameItem item)
        {
            if (string.IsNullOrEmpty(Options.Delimiter))
            {
                return value;
            }

            var tokens = value.Split(Options.Delimiter, StringSplitOptions.None);
            var count = tokens.Length;
            var sourceIndex = Options.TokenNumber - 1;
            var tokenNumberIsInvalid = Options.TokenNumber < 1 || sourceIndex >= count;
            if (tokenNumberIsInvalid)
            {
                return value;
            }

            var targetIndex = Math.Clamp(sourceIndex + Options.MoveBy, 0, count - 1);
            if (sourceIndex == targetIndex)
            {
                return value;
            }

            return _MoveToken(tokens, sourceIndex, targetIndex, Options.Delimiter);
        }

        private static string _MoveToken(string[] tokens, int sourceIndex, int targetIndex, string delimiter)
        {
            var list = new List<string>(tokens.Length);
            list.AddRange(tokens);
            var moved = list[sourceIndex];
            list.RemoveAt(sourceIndex);
            list.Insert(targetIndex, moved);
            return string.Join(delimiter, list);
        }
    }
}
