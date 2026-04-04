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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("XbX", f.Apply("aba", file));
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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("Xba", f.Apply("aba", file));
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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("X", f.Apply("foo", file));
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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("aNb", f.Apply("a12b", file));
        }
    }
}
