namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Anchoring side for position in string.
    /// </summary>
    public enum Side
    {
        Left,
        Right,
    }

    /// <summary>
    /// Represents a position anchored to a specific side of a string.
    /// </summary>
    /// <param name="Value">The index of the position, starting from 1.</param>
    /// <param name="Anchor">The side to which the position is anchored.</param>
    public sealed record Position(int Value, Side Anchor);

    /// <summary>
    /// Options for <see cref="TrimBetweenFilter"/>.
    /// </summary>
    /// <param name="Start">The start position of the trimming (inclusive).</param>
    /// <param name="End">The end position of the trimming (inclusive).</param>
    public sealed record TrimBetweenFilterOptions(Position Start, Position End);

    /// <summary>
    /// Removes a range of characters defined by start and end positions.
    /// <para>
    /// Both positions can be anchored to the left or right side of names.
    /// Positions are 1-based and inclusive.
    /// </para>
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Trimming options.</param>
    /// <param name="ApplyScope">When non-null, restricts this filter to a substring or token of the target; see <see cref="StringApplyScope"/>.</param>
    [FilterPalette(FilterGroup.Trimming, "Trim Between")]
    public sealed record TrimBetweenFilter(
        FilterTarget Target,
        TrimBetweenFilterOptions Options,
        StringApplyScope? ApplyScope = null
    ) : StringTargetFilter(Target, ApplyScope)
    {
        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (file prefix, positions 2–4 from left).
        /// </summary>
        public TrimBetweenFilter()
            : this(
                new FilePrefixTarget(),
                new TrimBetweenFilterOptions(Start: new Position(2, Side.Left), End: new Position(4, Side.Left))
            ) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimBetween";

        protected override string _TransformValue(string value, RenameItem item)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var startIndex = _GetAbsoluteIndex(Options.Start, value.Length);
            var endIndex = _GetAbsoluteIndex(Options.End, value.Length);

            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            return value.Remove(startIndex, endIndex - startIndex + 1);
        }

        /// <summary>
        /// Maps a 1-based left/right <see cref="Position"/> to a 0-based index in <paramref name="length"/>.
        /// </summary>
        /// <param name="position">Inclusive trim endpoint.</param>
        /// <param name="length">Non-zero string length (caller skips empty values).</param>
        /// <returns>Index clamped to <c>0..length-1</c>.</returns>
        private static int _GetAbsoluteIndex(Position position, int length)
        {
            var index = position.Anchor switch
            {
                Side.Left => position.Value - 1,
                Side.Right => length - position.Value,
                _ => throw new InvalidOperationException($"Unknown anchor side '{position.Anchor}'."),
            };

            return Math.Clamp(index, 0, length - 1);
        }
    }
}
