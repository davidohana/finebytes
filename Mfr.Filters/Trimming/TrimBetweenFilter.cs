using Mfr.Models;

namespace Mfr.Filters.Trimming
{
    /// <summary>
    /// Anchoring side for position in string.
    /// </summary>
    public enum Side
    {
        Left,
        Right
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
    public sealed record TrimBetweenFilter(
        FilterTarget Target,
        TrimBetweenFilterOptions Options) : BaseFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "TrimBetween";

        protected override string _TransformSegment(string segment, RenameItem item)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return segment;
            }

            var startIndex = _GetAbsoluteIndex(Options.Start, segment.Length);
            var endIndex = _GetAbsoluteIndex(Options.End, segment.Length);

            // Reorder if start is after end
            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            // Remove characters from startIndex to endIndex (inclusive)
            return segment.Remove(startIndex, endIndex - startIndex + 1);
        }

        private static int _GetAbsoluteIndex(Position position, int length)
        {
            var index = position.Anchor switch
            {
                Side.Left => position.Value - 1,
                Side.Right => length - position.Value,
                _ => throw new InvalidOperationException($"Unknown anchor side '{position.Anchor}'.")
            };

            return Math.Clamp(index, 0, length - 1);
        }
    }
}
