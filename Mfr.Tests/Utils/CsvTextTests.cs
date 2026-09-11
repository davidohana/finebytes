using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="CsvText"/>.
    /// </summary>
    public sealed class CsvTextTests
    {
        /// <summary>
        /// Verifies plain values are left unquoted.
        /// </summary>
        [Fact]
        public void EscapeField_plain_returns_unchanged()
        {
            Assert.Equal("hello", CsvText.EscapeField("hello"));
            Assert.Equal(string.Empty, CsvText.EscapeField(string.Empty));
            Assert.Equal(string.Empty, CsvText.EscapeField(null));
        }

        /// <summary>
        /// Verifies commas, quotes, and newlines force quoting with doubled quotes.
        /// </summary>
        /// <param name="value">Raw field.</param>
        /// <param name="expected">Escaped CSV field.</param>
        [Theory]
        [InlineData("a,b", "\"a,b\"")]
        [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
        [InlineData("line1\nline2", "\"line1\nline2\"")]
        [InlineData("line1\r\nline2", "\"line1\r\nline2\"")]
        public void EscapeField_special_chars_are_quoted(string value, string expected)
        {
            Assert.Equal(expected, CsvText.EscapeField(value));
        }

        /// <summary>
        /// Verifies FormatRow joins escaped fields with commas.
        /// </summary>
        [Fact]
        public void FormatRow_joins_escaped_fields()
        {
            Assert.Equal(string.Empty, CsvText.FormatRow([]));
            Assert.Equal("a", CsvText.FormatRow(["a"]));
            Assert.Equal("a,b", CsvText.FormatRow(["a", "b"]));
            Assert.Equal("\"a,b\",c", CsvText.FormatRow(["a,b", "c"]));
        }
    }
}
