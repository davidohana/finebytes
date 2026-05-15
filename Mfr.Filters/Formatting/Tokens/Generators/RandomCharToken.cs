using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Generators
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
        /// <exception cref="ArgumentException">Thrown when the argument is not exactly two comma-separated characters.</exception>
        public Formatter Compile(string tokenArgs)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            var segments = tokenArgs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Require.That(
                segments.Length == 2,
                $"Invalid {tokenDisplayName} arguments '{tokenArgs}'. Expected '{tokenDisplayName}:low,high' with two single-character endpoints.",
                nameof(tokenArgs));

            var lowSegment = segments[0];
            var highSegment = segments[1];
            Require.That(
                lowSegment.Length != 0 && highSegment.Length != 0,
                $"Invalid {tokenDisplayName} token: each endpoint must contain at least one character.",
                nameof(tokenArgs));

            var low = lowSegment[0];
            var high = highSegment[0];
            if (low > high)
                (low, high) = (high, low);

            var loCode = (int)low;
            var hiCode = (int)high;
            return _ => ((char)Random.Shared.Next(loCode, hiCode + 1)).ToString();
        }
    }
}
