using Mfr.Filters.Misc;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Misc
{
    /// <summary>
    /// Tests for <see cref="StripParenthesesFilter"/>.
    /// </summary>
    public class StripParenthesesFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies round parentheses and contents are removed.
        /// </summary>
        [Fact]
        public void Apply_RoundRemoveContents_RemovesParenthetical()
        {
            var f = new StripParenthesesFilter(
                true,
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: true));
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
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: false));
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
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Square, RemoveContents: true));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a[xx]b"));
        }

        /// <summary>
        /// Verifies curly brace stripping when removing contents.
        /// </summary>
        [Fact]
        public void Apply_CurlyRemoveContents_RemovesBracedRegion()
        {
            var f = new StripParenthesesFilter(
                true,
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Curly, RemoveContents: true));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a{xx}b"));
        }

        /// <summary>
        /// Verifies angle bracket stripping when removing contents.
        /// </summary>
        [Fact]
        public void Apply_AngleRemoveContents_RemovesAngleRegion()
        {
            var f = new StripParenthesesFilter(
                true,
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Angle, RemoveContents: true));
            Assert.Equal("ab", FilterTestHelpers.ApplyToPrefix(f, "a<xx>b"));
        }
    }
}
