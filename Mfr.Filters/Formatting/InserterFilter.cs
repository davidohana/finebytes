using Mfr.Models;

namespace Mfr.Filters.Formatting
{
    /// <summary>
    /// Selects whether the insert index counts from the start or end of the segment.
    /// </summary>
    public enum InserterOrigin
    {
        /// <summary>
        /// Counts from the first character; position <c>1</c> inserts before the first character.
        /// </summary>
        Beginning,

        /// <summary>
        /// Counts from the last character; position <c>1</c> inserts before the last character.
        /// </summary>
        End
    }

    /// <summary>
    /// Options for inserting resolved text at a fixed index.
    /// </summary>
    /// <param name="Text">Literal text or formatter template (angle-bracket tokens).</param>
    /// <param name="Position">One-based index; see <see cref="InserterOrigin"/>.</param>
    /// <param name="StartFrom">Whether <paramref name="Position"/> counts from the start or end of the segment.</param>
    /// <param name="Overwrite">If <see langword="true"/>, inserted text overwrites existing characters instead of shifting them.</param>
    public sealed record InserterOptions(
        string Text,
        int Position,
        InserterOrigin StartFrom,
        bool Overwrite);

    /// <summary>
    /// Inserts formatter-resolved text at a fixed position in the target segment.
    /// </summary>
    /// <param name="Target">The target that this filter applies to.</param>
    /// <param name="Options">Inserter options.</param>
    public sealed record InserterFilter(
        FilterTarget Target,
        InserterOptions Options) : BaseFilter(Target)
    {
        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Inserter";

        /// <inheritdoc />
        protected override string _TransformSegment(string segment, RenameItem item)
        {
            var inserted = FormatterTokenResolver.ResolveTemplate(Options.Text, item);
            if (inserted.Length == 0)
            {
                return segment;
            }

            var insertIndex = _ComputeInsertIndex(segment.Length, Options.Position, Options.StartFrom);
            if (Options.Overwrite)
            {
                return _OverwriteAt(segment, insertIndex, inserted);
            }

            return string.Concat(segment.AsSpan(0, insertIndex), inserted, segment.AsSpan(insertIndex));
        }

        private static string _OverwriteAt(string segment, int insertIndex, string inserted)
        {
            var remainderStart = insertIndex + inserted.Length;
            if (remainderStart >= segment.Length)
            {
                return string.Concat(segment.AsSpan(0, insertIndex), inserted);
            }

            return string.Concat(segment.AsSpan(0, insertIndex), inserted, segment.AsSpan(remainderStart));
        }

        private static int _ComputeInsertIndex(int length, int position, InserterOrigin startFrom)
        {
            var oneBased = position < 1 ? 1 : position;

            if (startFrom == InserterOrigin.Beginning)
            {
                var zeroBased = oneBased - 1;
                var exceedsLength = zeroBased > length;
                if (exceedsLength)
                {
                    return length;
                }

                return zeroBased;
            }

            if (oneBased > length)
            {
                return 0;
            }

            return length - oneBased;
        }
    }
}
