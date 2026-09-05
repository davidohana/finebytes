using Mfr.Filters.Replace;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="CleanerFilter"/>.
    /// </summary>
    public class CleanerFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies add-to-list defaults match MFR7 custom chars plus illegal-char cleanup on.
        /// </summary>
        [Fact]
        public void Ctor_Defaults_EnableIllegalCleanupAndMfr7CustomList()
        {
            var f = new CleanerFilter();
            Assert.IsType<FilePrefixTarget>(f.Target);
            Assert.True(f.Options.RemoveIllegalChars);
            Assert.Equal(@"!""#$%&'()*+,/:;<=>?@[]\^`{}|~", f.Options.CustomCharsToRemove);
            Assert.Equal(string.Empty, f.Options.Replacement);
        }

        /// <summary>
        /// Verifies illegal file-name characters are replaced.
        /// </summary>
        [Fact]
        public void Apply_RemoveIllegalChars_ReplacesInvalidCharacters()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: true, CustomCharsToRemove: "", Replacement: "_")
            );
            Assert.Equal("a_b", FilterTestHelpers.ApplyToPrefix(f, "a/b"));
        }

        /// <summary>
        /// Verifies Windows control characters are cleaned when illegal cleanup is on.
        /// </summary>
        [Fact]
        public void Apply_RemoveIllegalChars_ReplacesControlCharacters()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: true, CustomCharsToRemove: "", Replacement: "_")
            );
            Assert.Equal("a_b", FilterTestHelpers.ApplyToPrefix(f, "a\u0001b"));
        }

        /// <summary>
        /// Verifies custom character replacement.
        /// </summary>
        [Fact]
        public void Apply_CustomChars_ReplacesConfiguredCharacters()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: false, CustomCharsToRemove: "@#", Replacement: "-")
            );
            Assert.Equal("a-b-c", FilterTestHelpers.ApplyToPrefix(f, "a@b#c"));
        }

        /// <summary>
        /// Verifies both illegal and custom characters are replaced using the same replacement.
        /// </summary>
        [Fact]
        public void Apply_Both_ReplacesWithSameCharacter()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: true, CustomCharsToRemove: "@#", Replacement: "X")
            );
            Assert.Equal("aXbXcXdXe", FilterTestHelpers.ApplyToPrefix(f, "a/b@c#d|e"));
        }

        /// <summary>
        /// Verifies a multi-character replacement is inserted for each cleaned character.
        /// </summary>
        [Fact]
        public void Apply_MultiCharReplacement_InsertsFullStringPerMatch()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: false, CustomCharsToRemove: "@", Replacement: "xx")
            );
            Assert.Equal("axxbxxc", FilterTestHelpers.ApplyToPrefix(f, "a@b@c"));
        }

        /// <summary>
        /// Verifies an empty replacement deletes matched characters.
        /// </summary>
        [Fact]
        public void Apply_EmptyReplacement_DeletesCharacters()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: false, CustomCharsToRemove: "@#", Replacement: "")
            );
            Assert.Equal("abc", FilterTestHelpers.ApplyToPrefix(f, "a@b#c"));
        }

        /// <summary>
        /// Verifies a no-op when neither illegal nor custom characters are configured.
        /// </summary>
        [Fact]
        public void Apply_NothingToClean_ReturnsUnchanged()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: false, CustomCharsToRemove: "", Replacement: "_")
            );
            Assert.Equal("a/b@c", FilterTestHelpers.ApplyToPrefix(f, "a/b@c"));
        }

        /// <summary>
        /// Verifies replacement text is inserted as-is and not re-scanned for cleaned characters.
        /// </summary>
        [Fact]
        public void Apply_ReplacementContainingCleanedChars_DoesNotRescan()
        {
            var f = new CleanerFilter(
                _target,
                new CleanerOptions(RemoveIllegalChars: false, CustomCharsToRemove: "ab", Replacement: "ba")
            );
            Assert.Equal("ba", FilterTestHelpers.ApplyToPrefix(f, "a"));
        }
    }
}
