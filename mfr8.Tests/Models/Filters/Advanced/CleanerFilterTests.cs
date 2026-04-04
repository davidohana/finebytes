using Mfr8.Models;
using Mfr8.Models.Filters.Advanced;

namespace Mfr8.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="CleanerFilter"/>.
    /// </summary>
    public class CleanerFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("a_b", f.Apply("a<b", file));
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
            var file = FilterTestHelpers.CreateFile();
            Assert.Equal("a-b-c", f.Apply("a@b#c", file));
        }
    }
}
