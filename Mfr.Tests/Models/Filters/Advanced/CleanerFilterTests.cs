using Mfr.Models;
using Mfr.Models.Filters.Advanced;

namespace Mfr.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="CleanerFilter"/>.
    /// </summary>
    public class CleanerFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies illegal file-name characters are replaced.
        /// </summary>
        [Fact]
        public void Apply_RemoveIllegalChars_ReplacesInvalidCharacters()
        {
            var f = new CleanerFilter(
                true,
                _Target,
                new CleanerOptions(
                    RemoveIllegalChars: true,
                    IllegalCharReplacement: "_",
                    CustomCharsToRemove: "",
                    CustomReplacement: ""));
            Assert.Equal("a_b", FilterTestHelpers.ApplyToPrefix(f, "a/b"));
        }

        /// <summary>
        /// Verifies custom character replacement.
        /// </summary>
        [Fact]
        public void Apply_CustomChars_ReplacesConfiguredCharacters()
        {
            var f = new CleanerFilter(
                true,
                _Target,
                new CleanerOptions(
                    RemoveIllegalChars: false,
                    IllegalCharReplacement: "",
                    CustomCharsToRemove: "@#",
                    CustomReplacement: "-"));
            Assert.Equal("a-b-c", FilterTestHelpers.ApplyToPrefix(f, "a@b#c"));
        }
    }
}
