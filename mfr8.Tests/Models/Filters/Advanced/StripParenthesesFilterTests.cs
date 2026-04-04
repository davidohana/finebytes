using Mfr8.Models;
using Mfr8.Models.Filters.Advanced;

namespace Mfr8.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="StripParenthesesFilter"/>.
    /// </summary>
    public class StripParenthesesFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("ab", f.Apply("a(rem)b", file));
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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("arem", f.Apply("a(rem)", file));
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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("ab", f.Apply("a[xx]b", file));
        }
    }
}
