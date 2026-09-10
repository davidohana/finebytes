using Mfr.Utils;

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
            if (!TryGetSelectionRange(value, Options.Start, Options.End, out var startIndex, out var length))
            {
                return value;
            }

            return value.Remove(startIndex, length);
        }

        /// <summary>
        /// Maps start/end positions to a 0-based inclusive selection range in <paramref name="text"/>.
        /// </summary>
        /// <param name="text">Sample string (same value the filter would trim).</param>
        /// <param name="start">Inclusive start position.</param>
        /// <param name="end">Inclusive end position.</param>
        /// <param name="startIndex">0-based selection start when mapping succeeds.</param>
        /// <param name="length">Selection length when mapping succeeds.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="text"/> is non-empty and a range was computed; otherwise <see langword="false"/>.
        /// </returns>
        public static bool TryGetSelectionRange(
            string text,
            Position start,
            Position end,
            out int startIndex,
            out int length
        )
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(start);
            ArgumentNullException.ThrowIfNull(end);

            if (text.Length == 0)
            {
                startIndex = 0;
                length = 0;
                return false;
            }

            var from = _GetAbsoluteIndex(start, text.Length);
            var to = _GetAbsoluteIndex(end, text.Length);
            if (from > to)
            {
                (from, to) = (to, from);
            }

            startIndex = from;
            length = to - from + 1;
            return true;
        }

        /// <summary>
        /// Maps a non-empty 0-based selection to 1-based inclusive left-anchored positions.
        /// </summary>
        /// <param name="selectionStart">Selection start (0-based).</param>
        /// <param name="selectionLength">Selection length (must be &gt; 0).</param>
        /// <param name="start">Left-anchored inclusive start when mapping succeeds.</param>
        /// <param name="end">Left-anchored inclusive end when mapping succeeds.</param>
        /// <returns><see langword="true"/> when <paramref name="selectionLength"/> is positive.</returns>
        public static bool TryGetPositionsFromSelection(
            int selectionStart,
            int selectionLength,
            out Position start,
            out Position end
        )
        {
            if (selectionLength <= 0)
            {
                start = new Position(0, Side.Left);
                end = new Position(0, Side.Left);
                return false;
            }

            start = new Position(selectionStart + 1, Side.Left);
            end = new Position(selectionStart + selectionLength, Side.Left);
            return true;
        }

        /// <summary>
        /// Maps a 1-based left/right <see cref="Position"/> to a 0-based index in <paramref name="length"/>.
        /// </summary>
        /// <param name="position">Inclusive trim endpoint.</param>
        /// <param name="length">Non-zero string length (caller skips empty values).</param>
        /// <returns>Index clamped to <c>0..length-1</c>.</returns>
        private static int _GetAbsoluteIndex(Position position, int length)
        {
            return InclusiveStringPositions.ToZeroBasedIndex(
                position.Value,
                fromLeft: position.Anchor == Side.Left,
                length
            );
        }
    }
}
