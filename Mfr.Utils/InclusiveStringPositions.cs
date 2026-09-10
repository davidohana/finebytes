namespace Mfr.Utils
{
    /// <summary>
    /// Converts inclusive 1-based left/right string positions to 0-based indices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared by substring ApplyScope and Trim Between. Not for Inserter insert-before indices
    /// (different end semantics: position 1 from the end appends).
    /// </para>
    /// </remarks>
    public static class InclusiveStringPositions
    {
        /// <summary>
        /// Maps an inclusive 1-based position to a 0-based index in <c>[0, length - 1]</c>.
        /// </summary>
        /// <param name="oneBasedPosition">
        /// 1-based character position; values below 1 or above <paramref name="length"/> clamp to the
        /// corresponding edge for the chosen anchor.
        /// </param>
        /// <param name="fromLeft">When <see langword="true"/>, count from the start; otherwise from the end.</param>
        /// <param name="length">Non-zero string length (callers skip empty strings).</param>
        /// <returns>Clamped 0-based index.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="length"/> is not positive.</exception>
        public static int ToZeroBasedIndex(int oneBasedPosition, bool fromLeft, int length)
        {
            Require.That(length > 0, "length must be positive.", nameof(length));

            var index = fromLeft ? oneBasedPosition - 1 : length - oneBasedPosition;
            return Math.Clamp(index, 0, length - 1);
        }
    }
}
