using Mfr.Core;
using Mfr.Filters.Case;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="SentenceEndCharactersFilter"/>.
    /// </summary>
    public sealed class SentenceEndCharactersFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies the segment text is unchanged.
        /// </summary>
        [Fact]
        public void Apply_DoesNotChangeText()
        {
            var filter = new SentenceEndCharactersFilter(
                Target: _target,
                Options: new SentenceEndCharactersOptions(Characters: ":;"));

            var result = FilterTestHelpers.ApplyToPrefix(filter, "hello: world");

            Assert.Equal("hello: world", result);
        }

        /// <summary>
        /// Verifies <see cref="RenameItem.SentenceEndChars"/> is updated for later filters.
        /// </summary>
        [Fact]
        public void Apply_SetsSentenceEndCharsOnRenameItem()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(
                Target: _target,
                Options: new SentenceEndCharactersOptions(Characters: "-.!"));
            var lettersCaseFilter = new LettersCaseFilter(
                Target: _target,
                Options: new LettersCaseOptions(LettersCaseMode.SentenceCase, SkipWords: []));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a - b. c");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersCaseFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);

            Assert.Equal("A - B. C", item.Preview.Prefix);
        }
    }
}
