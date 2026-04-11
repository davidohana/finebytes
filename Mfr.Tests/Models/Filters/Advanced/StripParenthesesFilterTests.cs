using Mfr.Filters.Advanced;
using Mfr.Models;
using StripParenthesesFilter = Mfr.Filters.Advanced.StripParenthesesFilter;

namespace Mfr.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="StripParenthesesFilter"/>.
    /// </summary>
    public class StripParenthesesFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies round parentheses and contents are removed.
        /// </summary>
        [Fact]
        public void Apply_RoundRemoveContents_RemovesParenthetical()
        {
            var f = new StripParenthesesFilter(
                true,
                _Target,
                new StripParenthesesOptions(Types: "Round", RemoveContents: true));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a(rem)b"));
        }

        /// <summary>
        /// Verifies delimiters are removed but contents kept.
        /// </summary>
        [Fact]
        public void Apply_RoundKeepContents_RemovesOnlyDelimiters()
        {
            var f = new StripParenthesesFilter(
                true,
                _Target,
                new StripParenthesesOptions(Types: "Round", RemoveContents: false));
            Assert.Equal("arem", FilterTestHelpers.ApplyToPrefix(f, "a(rem)"));
        }

        /// <summary>
        /// Verifies square bracket stripping.
        /// </summary>
        [Fact]
        public void Apply_SquareRemoveContents_RemovesBracketed()
        {
            var f = new StripParenthesesFilter(
                true,
                _Target,
                new StripParenthesesOptions(Types: "Square", RemoveContents: true));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a[xx]b"));
        }
    }
}
