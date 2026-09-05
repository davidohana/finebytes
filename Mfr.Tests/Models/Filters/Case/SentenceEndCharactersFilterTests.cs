using Mfr.Filters.Case;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="SentenceEndCharactersFilter"/>.
    /// </summary>
    public sealed class SentenceEndCharactersFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies the segment text is unchanged while sentence-end chars are updated.
        /// </summary>
        [Fact]
        public void Apply_DoesNotChangeText_ButSetsSentenceEndChars()
        {
            var filter = new SentenceEndCharactersFilter(
                Target: _target,
                Options: new SentenceEndCharactersOptions(Characters: ":;")
            );

            var item = FilterTestHelpers.ApplyReturnItem(filter, "hello: world");

            Assert.Equal("hello: world", item.Preview.Prefix);
            Assert.Equal(":;", item.SentenceEndChars);
        }

        /// <summary>
        /// Verifies <see cref="RenameItem.SentenceEndChars"/> is consulted by later sentence-case filters.
        /// </summary>
        [Fact]
        public void Apply_SetsSentenceEndCharsForLaterSentenceCase()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(
                Target: _target,
                Options: new SentenceEndCharactersOptions(Characters: "-.!")
            );
            var lettersCaseFilter = new LettersCaseFilter(
                Target: _target,
                Options: new LettersCaseOptions(LettersCaseMode.SentenceCase, CapitalizeSkipWords: [])
            );
            var item = FilterTestHelpers.CreateRenameItem(prefix: "a - b. c");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersCaseFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("-.!", item.SentenceEndChars);
            Assert.Equal("A - B. C", item.Preview.Prefix);
        }
    }
}
