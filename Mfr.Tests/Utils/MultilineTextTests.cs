using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="MultilineText"/>.
    /// </summary>
    public sealed class MultilineTextTests
    {
        /// <summary>
        /// Verifies null or empty text yields no lines.
        /// </summary>
        /// <param name="text">Input under test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void EnumerateLines_null_or_empty_yields_nothing(string? text)
        {
            Assert.Empty(MultilineText.EnumerateLines(text));
        }

        /// <summary>
        /// Verifies LF, CR, and CRLF are accepted and interior blanks are kept.
        /// </summary>
        [Fact]
        public void EnumerateLines_preserves_interior_blanks_and_accepts_crlf()
        {
            var lines = MultilineText.EnumerateLines("A\r\n\r\nB\nC\rD").ToArray();

            Assert.Equal(["A", "", "B", "C", "D"], lines);
        }

        /// <summary>
        /// Verifies a trailing newline after the last line does not add an empty entry.
        /// </summary>
        [Fact]
        public void EnumerateLines_trailing_newline_does_not_add_entry()
        {
            var lines = MultilineText.EnumerateLines("Alpha\nBeta\n").ToArray();

            Assert.Equal(["Alpha", "Beta"], lines);
        }
    }
}
