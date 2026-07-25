using Mfr.Utils;

namespace Mfr.Tests.Utils
{
    /// <summary>
    /// Tests for <see cref="StringExtensions"/>.
    /// </summary>
    public sealed class StringExtensionsTests
    {
        /// <summary>
        /// Verifies null, empty, and whitespace-only text counts as blank.
        /// </summary>
        /// <param name="value">Text under test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t\r\n")]
        public void IsBlank_blank_text_returns_true(string? value)
        {
            Assert.True(value.IsBlank());
        }

        /// <summary>
        /// Verifies text with any non-whitespace character is not blank.
        /// </summary>
        [Fact]
        public void IsBlank_text_with_content_returns_false()
        {
            Assert.False(" x ".IsBlank());
        }

        /// <summary>
        /// Verifies blank text is normalized to absent, the convention tag fields rely on.
        /// </summary>
        /// <param name="value">Text under test.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void TrimmedOrNull_blank_returns_null(string? value)
        {
            Assert.Null(value.TrimmedOrNull());
        }

        /// <summary>
        /// Verifies surrounding whitespace is removed from text that has content.
        /// </summary>
        [Fact]
        public void TrimmedOrNull_trims_surrounding_whitespace()
        {
            Assert.Equal("Alice", "  Alice \t".TrimmedOrNull());
        }

        /// <summary>
        /// Verifies inner whitespace is preserved.
        /// </summary>
        [Fact]
        public void TrimmedOrNull_keeps_inner_whitespace()
        {
            Assert.Equal("Alice  and Bob", " Alice  and Bob ".TrimmedOrNull());
        }
    }
}
