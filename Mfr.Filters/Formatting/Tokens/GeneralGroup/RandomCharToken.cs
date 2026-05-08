using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Resolves the <c>&lt;random-char:low,high&gt;</c> token to one uniformly random character between endpoints (inclusive).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each endpoint must be a single character (the first code unit is used). If <c>low</c> is greater than <c>high</c>
    /// after trimming, the bounds are swapped. Any characters work—for example <c>A,Z</c>, <c>0,9</c>, or <c>a,z</c>.
    /// </para>
    /// </remarks>
    internal sealed class RandomCharToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["random-char"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when the argument is not exactly two comma-separated characters.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            var segments = arg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2)
            {
                throw new InvalidOperationException(
                    $"Invalid random-char token arg '{arg}'. Expected '<random-char:low,high>' with two single-character endpoints.");
            }

            var lowSegment = segments[0];
            var highSegment = segments[1];
            if (lowSegment.Length == 0 || highSegment.Length == 0)
            {
                throw new InvalidOperationException(
                    "Invalid random-char token: each endpoint must contain at least one character.");
            }

            var low = lowSegment[0];
            var high = highSegment[0];
            if (low > high)
                (low, high) = (high, low);

            var loCode = (int)low;
            var hiCode = (int)high;
            var picked = (char)Random.Shared.Next(loCode, hiCode + 1);
            return picked.ToString();
        }
    }
}
