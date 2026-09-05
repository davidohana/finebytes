using Mfr.Filters.Misc;

namespace Mfr.Tests.Models.Filters.Misc
{
    /// <summary>
    /// Tests for <see cref="StripParenthesesFilter"/>.
    /// </summary>
    public class StripParenthesesFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies each bracket type removes matched pairs and contents.
        /// </summary>
        [Theory]
        [InlineData(ParenthesisType.Round, "a(rem)b", "ab")]
        [InlineData(ParenthesisType.Square, "a[xx]b", "ab")]
        [InlineData(ParenthesisType.Curly, "a{xx}b", "ab")]
        [InlineData(ParenthesisType.Angle, "a<xx>b", "ab")]
        public void Apply_RemoveContents_RemovesMatchedRegion(ParenthesisType type, string input, string expected)
        {
            var f = new StripParenthesesFilter(_target, new StripParenthesesOptions(Type: type, RemoveContents: true));
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, input));
        }

        /// <summary>
        /// Verifies delimiters are removed but contents kept.
        /// </summary>
        [Fact]
        public void Apply_RoundKeepContents_RemovesOnlyDelimiters()
        {
            var f = new StripParenthesesFilter(
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: false)
            );
            Assert.Equal("arem", FilterTestHelpers.ApplyToPrefix(f, "a(rem)"));
        }

        /// <summary>
        /// Verifies nested pairs are stripped innermost-first (MFR7 parity).
        /// </summary>
        [Fact]
        public void Apply_NestedRemoveContents_RemovesAllMatchedRegions()
        {
            var f = new StripParenthesesFilter(
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: true)
            );
            Assert.Equal("ae", FilterTestHelpers.ApplyToPrefix(f, "a(b(c)d)e"));
        }

        /// <summary>
        /// Verifies nested pairs keep interior text when only delimiters are stripped.
        /// </summary>
        [Fact]
        public void Apply_NestedKeepContents_RemovesOnlyDelimiters()
        {
            var f = new StripParenthesesFilter(
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: false)
            );
            Assert.Equal("abcde", FilterTestHelpers.ApplyToPrefix(f, "a(b(c)d)e"));
        }

        /// <summary>
        /// Verifies multiple disjoint pairs are all stripped.
        /// </summary>
        [Fact]
        public void Apply_MultiplePairsRemoveContents_RemovesEachRegion()
        {
            var f = new StripParenthesesFilter(
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: true)
            );
            Assert.Equal("ace", FilterTestHelpers.ApplyToPrefix(f, "a(b)c(d)e"));
        }

        /// <summary>
        /// Verifies unmatched delimiters are left alone when removing contents.
        /// </summary>
        [Fact]
        public void Apply_UnmatchedRemoveContents_LeavesUnmatchedDelimiters()
        {
            var f = new StripParenthesesFilter(
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: true)
            );
            Assert.Equal("a(b", FilterTestHelpers.ApplyToPrefix(f, "a(b"));
            Assert.Equal("a)b", FilterTestHelpers.ApplyToPrefix(f, "a)b"));
            Assert.Equal("a(bd", FilterTestHelpers.ApplyToPrefix(f, "a(b(c)d"));
            Assert.Equal("ac)", FilterTestHelpers.ApplyToPrefix(f, "a(b)c)"));
        }

        /// <summary>
        /// Verifies unmatched delimiters are left alone when keeping contents.
        /// </summary>
        [Fact]
        public void Apply_UnmatchedKeepContents_LeavesUnmatchedDelimiters()
        {
            var f = new StripParenthesesFilter(
                _target,
                new StripParenthesesOptions(Type: ParenthesisType.Round, RemoveContents: false)
            );
            Assert.Equal("a(bcd", FilterTestHelpers.ApplyToPrefix(f, "a(b(c)d"));
            Assert.Equal("abc)", FilterTestHelpers.ApplyToPrefix(f, "a(b)c)"));
        }
    }
}
