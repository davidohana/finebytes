using Mfr.Filters.Replace;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="CleanerFilter"/>.
    /// </summary>
    public class CleanerFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies illegal file-name characters are replaced.
        /// </summary>
        [Fact]
        public void Apply_RemoveIllegalChars_ReplacesInvalidCharacters()
        {
            var f = new CleanerFilter(
                true,
                _target,
                new CleanerOptions(
                    RemoveIllegalChars: true,
                    CustomCharsToRemove: "",
                    Replacement: "_"));
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
                _target,
                new CleanerOptions(
                    RemoveIllegalChars: false,
                    CustomCharsToRemove: "@#",
                    Replacement: "-"));
            Assert.Equal("a-b-c", FilterTestHelpers.ApplyToPrefix(f, "a@b#c"));
        }

        /// <summary>
        /// Verifies both illegal and custom characters are replaced using the same replacement.
        /// </summary>
        [Fact]
        public void Apply_Both_ReplacesWithSameCharacter()
        {
            var f = new CleanerFilter(
                true,
                _target,
                new CleanerOptions(
                    RemoveIllegalChars: true,
                    CustomCharsToRemove: "@#",
                    Replacement: "X"));
            Assert.Equal("aXbXcXdXe", FilterTestHelpers.ApplyToPrefix(f, "a/b@c#d|e"));
        }
    }
}
