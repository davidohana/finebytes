using Mfr.Filters.Trimming;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Trimming
{
    /// <summary>
    /// Tests for <see cref="ShrinkDuplicateCharactersFilter"/>.
    /// </summary>
    public class ShrinkDuplicateCharactersFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies adjacent duplicate occurrences of the configured character collapse to one.
        /// </summary>
        [Fact]
        public void Apply_CollapsesAdjacentDuplicatesOfConfiguredCharacter()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                                _target,
                new ShrinkDuplicateCharactersOptions(Character: '-'));

            Assert.Equal("I am Kloot - To You", FilterTestHelpers.ApplyToPrefix(filter, "I am Kloot --- To You"));
            Assert.Equal("a-b-c", FilterTestHelpers.ApplyToPrefix(filter, "a--b---c"));
        }

        /// <summary>
        /// Verifies only adjacent duplicates are affected and non-adjacent occurrences are retained.
        /// </summary>
        [Fact]
        public void Apply_LeavesNonAdjacentOccurrencesUntouched()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                                _target,
                new ShrinkDuplicateCharactersOptions(Character: '>'));

            Assert.Equal("a>b>c", FilterTestHelpers.ApplyToPrefix(filter, "a>>b>>>c"));
            Assert.Equal(">a>b>", FilterTestHelpers.ApplyToPrefix(filter, ">>>a>>>b>>>"));
        }

        /// <summary>
        /// Verifies unchanged output when the configured character is absent.
        /// </summary>
        [Fact]
        public void Apply_NoTargetCharacter_ReturnsInputAsIs()
        {
            var filter = new ShrinkDuplicateCharactersFilter(
                                _target,
                new ShrinkDuplicateCharactersOptions(Character: '-'));

            Assert.Equal("abc def", FilterTestHelpers.ApplyToPrefix(filter, "abc def"));
        }
    }
}
