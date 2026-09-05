using Mfr.Filters.Replace;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplacerFilter"/>.
    /// </summary>
    public class ReplacerFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies literal replacement for replace-all and replace-once modes.
        /// </summary>
        [Theory]
        [InlineData(true, "XbX")]
        [InlineData(false, "Xba")]
        public void Apply_LiteralReplacement_RespectsReplaceAllOption(bool replaceAll, string expected)
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    "a",
                    "X",
                    ReplacerMode.Literal,
                    CaseSensitive: true,
                    ReplaceAll: replaceAll,
                    WholeWord: false
                )
            );
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, "aba"));
        }

        /// <summary>
        /// Verifies wildcard mode.
        /// </summary>
        [Fact]
        public void Apply_Wildcard_ReplacesPattern()
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    "f*o",
                    "X",
                    ReplacerMode.Wildcard,
                    CaseSensitive: true,
                    ReplaceAll: true,
                    WholeWord: false
                )
            );
            Assert.Equal("X", FilterTestHelpers.ApplyToPrefix(f, "foo"));
        }

        /// <summary>
        /// Verifies regex replacement for replace-all and replace-once modes.
        /// </summary>
        [Theory]
        [InlineData(true, "aNbcN")]
        [InlineData(false, "aNbc34")]
        public void Apply_RegexReplacement_RespectsReplaceAllOption(bool replaceAll, string expected)
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    @"\d+",
                    "N",
                    ReplacerMode.Regex,
                    CaseSensitive: true,
                    ReplaceAll: replaceAll,
                    WholeWord: false
                )
            );
            Assert.Equal(expected, FilterTestHelpers.ApplyToPrefix(f, "a12bc34"));
        }

        /// <summary>
        /// Verifies case-insensitive matching.
        /// </summary>
        [Fact]
        public void Apply_LiteralIgnoreCase_ReplacesRegardlessOfCasing()
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    "a",
                    "X",
                    ReplacerMode.Literal,
                    CaseSensitive: false,
                    ReplaceAll: true,
                    WholeWord: false
                )
            );
            Assert.Equal("XbX", FilterTestHelpers.ApplyToPrefix(f, "AbA"));
        }

        /// <summary>
        /// Verifies whole word matching.
        /// </summary>
        [Fact]
        public void Apply_WholeWord_ReplacesOnlyWholeWords()
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    "cat",
                    "dog",
                    ReplacerMode.Literal,
                    CaseSensitive: true,
                    ReplaceAll: true,
                    WholeWord: true
                )
            );
            Assert.Equal("dog", FilterTestHelpers.ApplyToPrefix(f, "cat"));
            Assert.Equal("category", FilterTestHelpers.ApplyToPrefix(f, "category"));
            Assert.Equal("a dog b", FilterTestHelpers.ApplyToPrefix(f, "a cat b"));
        }

        /// <summary>
        /// Verifies '?' wildcard.
        /// </summary>
        [Fact]
        public void Apply_WildcardQuestionMark_ReplacesSingleCharacter()
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    "f?o",
                    "X",
                    ReplacerMode.Wildcard,
                    CaseSensitive: true,
                    ReplaceAll: true,
                    WholeWord: false
                )
            );
            Assert.Equal("X", FilterTestHelpers.ApplyToPrefix(f, "foo"));
            Assert.Equal("X", FilterTestHelpers.ApplyToPrefix(f, "fao"));
        }

        /// <summary>
        /// Verifies the combination of Case-Insensitive and Whole-Word.
        /// </summary>
        [Fact]
        public void Apply_IgnoreCaseWholeWord_WorksCorrectly()
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    "CAT",
                    "dog",
                    ReplacerMode.Literal,
                    CaseSensitive: false,
                    ReplaceAll: true,
                    WholeWord: true
                )
            );
            Assert.Equal("dog", FilterTestHelpers.ApplyToPrefix(f, "cat"));
            Assert.Equal("Category", FilterTestHelpers.ApplyToPrefix(f, "Category"));
        }

        /// <summary>
        /// Verifies an empty find pattern is a no-op in every mode (MFR7; empty regex would match every position).
        /// </summary>
        [Theory]
        [InlineData(ReplacerMode.Literal)]
        [InlineData(ReplacerMode.Wildcard)]
        [InlineData(ReplacerMode.Regex)]
        public void Apply_EmptyFind_IsNoOp(ReplacerMode mode)
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions("", "X", mode, CaseSensitive: true, ReplaceAll: true, WholeWord: false)
            );
            Assert.Equal("aba", FilterTestHelpers.ApplyToPrefix(f, "aba"));
        }

        /// <summary>
        /// Verifies Literal/Wildcard insert <c>$</c> in the replacement as plain text, not regex substitutions.
        /// </summary>
        [Theory]
        [InlineData(ReplacerMode.Literal)]
        [InlineData(ReplacerMode.Wildcard)]
        public void Apply_NonRegexMode_TreatsDollarInReplacementAsLiteral(ReplacerMode mode)
        {
            var find = mode == ReplacerMode.Wildcard ? "a*" : "a";
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(find, "$1", mode, CaseSensitive: true, ReplaceAll: true, WholeWord: false)
            );
            Assert.Equal("$1", FilterTestHelpers.ApplyToPrefix(f, "a"));
        }

        /// <summary>
        /// Verifies Regex mode still expands substitution references in the replacement.
        /// </summary>
        [Fact]
        public void Apply_RegexMode_ExpandsReplacementGroups()
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    @"(a)(b)",
                    "$2$1",
                    ReplacerMode.Regex,
                    CaseSensitive: true,
                    ReplaceAll: true,
                    WholeWord: false
                )
            );
            Assert.Equal("ba", FilterTestHelpers.ApplyToPrefix(f, "ab"));
        }

        /// <summary>
        /// Verifies invalid regex patterns fail during setup (preview marks all items).
        /// </summary>
        [Fact]
        public void Setup_InvalidRegex_ThrowsArgumentException()
        {
            var f = new ReplacerFilter(
                _target,
                new ReplacerOptions(
                    "(",
                    "X",
                    ReplacerMode.Regex,
                    CaseSensitive: true,
                    ReplaceAll: true,
                    WholeWord: false
                )
            );
            var ex = Assert.Throws<ArgumentException>(f.Setup);
            Assert.Contains("Invalid regular expression", ex.Message, StringComparison.Ordinal);
        }
    }
}
