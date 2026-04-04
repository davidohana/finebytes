using Mfr.Models;
using Mfr.Models.Filters.Advanced;

namespace Mfr.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="ReplacerFilter"/>.
    /// </summary>
    public class ReplacerFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

        /// <summary>
        /// Verifies literal replace-all.
        /// </summary>
        [Fact]
        public void Apply_LiteralReplaceAll_ReplacesEveryOccurrence()
        {
            var f = new ReplacerFilter(
                true,
                _Target,
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
                _Target,
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
                _Target,
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
                _Target,
                new ReplacerOptions(@"\d+", "N", ReplacerMode.Regex, CaseSensitive: true, ReplaceAll: true, WholeWord: false));
            Assert.Equal("aNb", FilterTestHelpers.ApplyToPrefix(f, "a12b"));
        }
    }
}
