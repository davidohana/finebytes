using Mfr.Filters;
using Mfr.Filters.Case;
using Mfr.Filters.Space;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="CasingListFilter"/>.
    /// </summary>
    public sealed class CasingListFilterTests
    {
        private static readonly FileNameTarget _target = new();

        private static readonly string[] _sampleWords = ["and", "or", "with", "RMX"];

        /// <summary>
        /// Verifies add-to-list defaults keep an empty word list (factory Load defaults is separate).
        /// </summary>
        [Fact]
        public void ParameterlessCtor_UsesEmptyWords()
        {
            var filter = new CasingListFilter();

            Assert.Empty(filter.Options.Words);
            Assert.True(filter.Options.UppercaseSentenceInitial);
        }

        /// <summary>
        /// Verifies curated DefaultWords build a valid casing map within list limits.
        /// </summary>
        [Fact]
        public void DefaultWords_BuildsMapWithinListEntryLimits()
        {
            var words = CasingListOptions.DefaultWords;

            Assert.NotEmpty(words);
            Assert.True(words.Count <= ListEntryLength.DefaultMaxEntryCount);
            Assert.All(words, word => Assert.True(word.Length <= ListEntryLength.DefaultMaxLength));
            Assert.Equal(words.Count, words.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            var map = CasingListParser.BuildMap(words);
            Assert.Equal(words.Count, map.Count);
        }

        /// <summary>
        /// Verifies casing-list words are applied and unknown words remain unchanged.
        /// </summary>
        [Fact]
        public void Apply_UsesCasingListAndLeavesUnknownWordsUnchanged()
        {
            var filter = _CreateFilter(words: _sampleWords, uppercaseSentenceInitial: false);

            var result = FilterTestHelpers.ApplyToFileName(filter, "03 - WiTH Or Without You Rmx");

            Assert.Equal("03 - with or Without You RMX", result);
        }

        /// <summary>
        /// Verifies sentence-initial uppercase and custom sentence-end characters.
        /// </summary>
        [Fact]
        public void Apply_WithUppercaseSentenceInitial_UppercasesAfterSentenceBoundaries()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(
                Options: new SentenceEndCharactersOptions(Characters: "-.!")
            );
            var casingFilter = _CreateFilter(words: _sampleWords, uppercaseSentenceInitial: true);
            var item = FilterTestHelpers.CreateRenameItem(fileName: "03 - WiTH Or Without You Rmx");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, casingFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("03 - With or Without You RMX", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies configured word separator from SpaceCharacter is respected.
        /// </summary>
        [Fact]
        public void Apply_AfterSpaceCharacter_UsesConfiguredWordSeparator()
        {
            var spaceCharacterFilter = new SpaceCharacterFilter(
                Target: _target,
                Options: new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: [" "])
            );
            var casingFilter = _CreateFilter(words: ["and", "us", "them"], uppercaseSentenceInitial: true);
            var chain = FilterChain.CreateAllEnabled([spaceCharacterFilter, casingFilter]);

            var item = FilterTestHelpers.CreateRenameItem(fileName: "US_AND_THEM");
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("Us_and_them", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies an empty word list leaves the segment unchanged when sentence-initial is off.
        /// </summary>
        [Fact]
        public void Apply_EmptyWords_IsNoOp()
        {
            var filter = _CreateFilter(words: [], uppercaseSentenceInitial: false);

            var result = FilterTestHelpers.ApplyToFileName(filter, "WiTH Or Without");

            Assert.Equal("WiTH Or Without", result);
        }

        /// <summary>
        /// Verifies an empty word list still applies sentence-initial uppercasing when enabled.
        /// </summary>
        [Fact]
        public void Apply_EmptyWords_WithUppercaseSentenceInitial_UppercasesStart()
        {
            var filter = _CreateFilter(words: [], uppercaseSentenceInitial: true);

            var result = FilterTestHelpers.ApplyToFileName(filter, "hello world");

            Assert.Equal("Hello world", result);
        }

        /// <summary>
        /// Verifies sentence-initial mode does not capitalize after punctuation without a separator.
        /// </summary>
        [Fact]
        public void Apply_WithUppercaseSentenceInitial_RequiresSeparatorAfterSentenceEnd()
        {
            var filter = _CreateFilter(words: [], uppercaseSentenceInitial: true);

            var result = FilterTestHelpers.ApplyToFileName(filter, "hello.world");

            Assert.Equal("Hello.world", result);
        }

        /// <summary>
        /// Verifies sentence-initial mode uppercases non-ASCII letters.
        /// </summary>
        [Fact]
        public void Apply_WithUppercaseSentenceInitial_CapitalizesNonAsciiLetters()
        {
            var filter = _CreateFilter(words: [], uppercaseSentenceInitial: true);

            var result = FilterTestHelpers.ApplyToFileName(filter, "école. über next");

            Assert.Equal("École. Über next", result);
        }

        /// <summary>
        /// Verifies setup fails when a configured word contains a space.
        /// </summary>
        [Fact]
        public void Setup_WordWithSpace_ThrowsUserException()
        {
            var filter = _CreateFilter(words: ["ok", "not ok"], uppercaseSentenceInitial: false);

            var ex = Assert.Throws<UserException>(filter.Setup);
            Assert.Contains("single word", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies duplicate words use the last configured spelling.
        /// </summary>
        [Fact]
        public void Apply_DuplicateWords_LastWins()
        {
            var filter = _CreateFilter(words: ["foo", "FOO"], uppercaseSentenceInitial: false);

            var result = FilterTestHelpers.ApplyToFileName(filter, "Foo");

            Assert.Equal("FOO", result);
        }

        private static CasingListFilter _CreateFilter(IReadOnlyList<string> words, bool uppercaseSentenceInitial)
        {
            var options = new CasingListOptions(Words: words, UppercaseSentenceInitial: uppercaseSentenceInitial);
            return new CasingListFilter(Target: _target, Options: options);
        }
    }
}
