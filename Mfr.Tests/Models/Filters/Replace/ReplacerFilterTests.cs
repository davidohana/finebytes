using Mfr.Filters.Replace;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplacerFilter"/>.
    /// </summary>
    public class ReplacerFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies literal replace-all.
        /// </summary>
        [Fact]
        public void Apply_LiteralReplaceAll_ReplacesEveryOccurrence()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions("a", "X", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: false));
            Assert.Equal("XbX", FilterTestHelpers.ApplyToPrefix(f, "aba"));
        }

        /// <summary>
        /// Verifies literal single replace.
        /// </summary>
        [Fact]
        public void Apply_LiteralReplaceOnce_ReplacesFirstMatchOnly()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions("a", "X", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: false, WholeWord: false));
            Assert.Equal("Xba", FilterTestHelpers.ApplyToPrefix(f, "aba"));
        }

        /// <summary>
        /// Verifies wildcard mode.
        /// </summary>
        [Fact]
        public void Apply_Wildcard_ReplacesPattern()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions("f*o", "X", ReplacerMode.Wildcard, CaseSensitive: true, ReplaceAll: true, WholeWord: false));
            Assert.Equal("X", FilterTestHelpers.ApplyToPrefix(f, "foo"));
        }

        /// <summary>
        /// Verifies regex mode.
        /// </summary>
        [Fact]
        public void Apply_Regex_UsesPattern()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions(@"\d+", "N", ReplacerMode.Regex, CaseSensitive: true, ReplaceAll: true, WholeWord: false));
            Assert.Equal("aNb", FilterTestHelpers.ApplyToPrefix(f, "a12b"));
        }

        /// <summary>
        /// Verifies case-insensitive matching.
        /// </summary>
        [Fact]
        public void Apply_LiteralIgnoreCase_ReplacesRegardlessOfCasing()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions("a", "X", ReplacerMode.Literal, CaseSensitive: false, ReplaceAll: true, WholeWord: false));
            Assert.Equal("XbX", FilterTestHelpers.ApplyToPrefix(f, "AbA"));
        }

        /// <summary>
        /// Verifies whole word matching.
        /// </summary>
        [Fact]
        public void Apply_WholeWord_ReplacesOnlyWholeWords()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions("cat", "dog", ReplacerMode.Literal, CaseSensitive: true, ReplaceAll: true, WholeWord: true));
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
                true,
                _target,
                new ReplacerOptions("f?o", "X", ReplacerMode.Wildcard, CaseSensitive: true, ReplaceAll: true, WholeWord: false));
            Assert.Equal("X", FilterTestHelpers.ApplyToPrefix(f, "foo"));
            Assert.Equal("X", FilterTestHelpers.ApplyToPrefix(f, "fao"));
        }

        /// <summary>
        /// Verifies regex single replacement.
        /// </summary>
        [Fact]
        public void Apply_RegexReplaceOnce_ReplacesFirstMatchOnly()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions(@"\d+", "N", ReplacerMode.Regex, CaseSensitive: true, ReplaceAll: false, WholeWord: false));
            Assert.Equal("Nab2", FilterTestHelpers.ApplyToPrefix(f, "1ab2"));
        }

        /// <summary>
        /// Verifies the combination of Case-Insensitive and Whole-Word.
        /// </summary>
        [Fact]
        public void Apply_IgnoreCaseWholeWord_WorksCorrectly()
        {
            var f = new ReplacerFilter(
                true,
                _target,
                new ReplacerOptions("CAT", "dog", ReplacerMode.Literal, CaseSensitive: false, ReplaceAll: true, WholeWord: true));
            Assert.Equal("dog", FilterTestHelpers.ApplyToPrefix(f, "cat"));
            Assert.Equal("Category", FilterTestHelpers.ApplyToPrefix(f, "Category"));
        }
    }
}
