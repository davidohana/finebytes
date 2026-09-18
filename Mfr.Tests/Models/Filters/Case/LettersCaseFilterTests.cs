using Mfr.Filters.Case;
using Mfr.Filters.Space;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="LettersCaseFilter"/> transformations.
    /// </summary>
    public class LettersCaseFilterTests
    {
        private static readonly FileNameTarget _target = new();

        /// <summary>
        /// Verifies add-to-list defaults use capitalize mode and common skip words.
        /// </summary>
        [Fact]
        public void ParameterlessCtor_UsesCapitalizeAndDefaultSkipWords()
        {
            var f = new LettersCaseFilter();
            Assert.Equal(LettersCaseMode.Capitalize, f.Options.Mode);
            Assert.Equal(LettersCaseOptions.DefaultCapitalizeSkipWords, f.Options.CapitalizeSkipWords);
            Assert.Equal("a Song for the World", FilterTestHelpers.ApplyToFileName(f, "a song for the world"));
        }

        /// <summary>
        /// Verifies capitalize skip-words are the shared title-case exception array (not a drifted copy).
        /// </summary>
        [Fact]
        public void DefaultCapitalizeSkipWords_IsSharedTitleCaseExceptionArray()
        {
            var skipWords = LettersCaseOptions.DefaultCapitalizeSkipWords;
            Assert.Same(CommonCasingWords.TitleCaseExceptions, skipWords);
            Assert.Same(CommonCasingWords.DefaultWords, CasingListOptions.DefaultWords);
            Assert.Equal(skipWords, CasingListOptions.DefaultWords.Take(skipWords.Count));
        }

        /// <summary>
        /// Verifies upper-case mode.
        /// </summary>
        [Fact]
        public void Apply_UpperCase_ConvertsToUpperInvariant()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.UpperCase, []));
            Assert.Equal("HELLO", FilterTestHelpers.ApplyToFileName(f, "hello"));
        }

        /// <summary>
        /// Verifies lower-case mode.
        /// </summary>
        [Fact]
        public void Apply_LowerCase_ConvertsToLowerInvariant()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.LowerCase, []));
            Assert.Equal("hello", FilterTestHelpers.ApplyToFileName(f, "HELLO"));
        }

        /// <summary>
        /// Verifies first-letter-up mode uppercases the first letter and lowercases remaining letters.
        /// </summary>
        [Fact]
        public void Apply_FirstLetterUp_UppercasesFirstLetterOnly()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.FirstLetterUp, []));
            Assert.Equal("Hello world", FilterTestHelpers.ApplyToFileName(f, "hELLO world"));
        }

        /// <summary>
        /// Verifies first-letter-up mode applies to index 0 and lowercases the remainder.
        /// </summary>
        [Fact]
        public void Apply_FirstLetterUp_UsesIndexZero()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.FirstLetterUp, []));
            Assert.Equal(" 123_abc", FilterTestHelpers.ApplyToFileName(f, " 123_aBC"));
        }

        /// <summary>
        /// Verifies weird-case with 0% chance lowercases all letters.
        /// </summary>
        [Fact]
        public void Apply_WeirdCase_ZeroPercentUppercasesNone()
        {
            var f = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(
                    Mode: LettersCaseMode.WeirdCase,
                    CapitalizeSkipWords: [],
                    WeirdUppercaseChancePercent: 0,
                    WeirdFixedPlaces: false
                )
            );
            Assert.Equal("abc xyz", FilterTestHelpers.ApplyToFileName(f, "AbC XyZ"));
        }

        /// <summary>
        /// Verifies weird-case with 100% chance uppercases all letters.
        /// </summary>
        [Fact]
        public void Apply_WeirdCase_HundredPercentUppercasesAll()
        {
            var f = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(
                    Mode: LettersCaseMode.WeirdCase,
                    CapitalizeSkipWords: [],
                    WeirdUppercaseChancePercent: 100,
                    WeirdFixedPlaces: false
                )
            );
            Assert.Equal("ABC XYZ", FilterTestHelpers.ApplyToFileName(f, "AbC XyZ"));
        }

        /// <summary>
        /// Verifies weird-case fixed places keep the same uppercase/lowercase positions across names.
        /// </summary>
        [Fact]
        public void Apply_WeirdCase_FixedPlaces_UsesSamePositionsAcrossNames()
        {
            var f = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(
                    Mode: LettersCaseMode.WeirdCase,
                    CapitalizeSkipWords: [],
                    WeirdUppercaseChancePercent: 50,
                    WeirdFixedPlaces: true
                )
            );
            var a = FilterTestHelpers.ApplyToFileName(f, "abcdefgh", renameListIndex: 0);
            var b = FilterTestHelpers.ApplyToFileName(f, "qrstuvwx", renameListIndex: 999);

            Assert.Equal(_BuildUpperMask(a), _BuildUpperMask(b));
        }

        /// <summary>
        /// Verifies weird-case without fixed places varies by rename-list index.
        /// </summary>
        [Fact]
        public void Apply_WeirdCase_WithoutFixedPlaces_VariesByRenameListIndex()
        {
            var f = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(
                    Mode: LettersCaseMode.WeirdCase,
                    CapitalizeSkipWords: [],
                    WeirdUppercaseChancePercent: 50,
                    WeirdFixedPlaces: false
                )
            );
            var a = FilterTestHelpers.ApplyToFileName(f, "abcdefgh", renameListIndex: 0);
            var b = FilterTestHelpers.ApplyToFileName(f, "abcdefgh", renameListIndex: 999);

            Assert.NotEqual(_BuildUpperMask(a), _BuildUpperMask(b));
        }

        /// <summary>
        /// Verifies weird-case clamps out-of-range chance percents at apply time.
        /// </summary>
        [Theory]
        [InlineData(-5, "abc xyz")]
        [InlineData(150, "ABC XYZ")]
        public void Apply_WeirdCase_ClampsChancePercent(int chancePercent, string expected)
        {
            var f = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(
                    Mode: LettersCaseMode.WeirdCase,
                    CapitalizeSkipWords: [],
                    WeirdUppercaseChancePercent: chancePercent,
                    WeirdFixedPlaces: false
                )
            );
            Assert.Equal(expected, FilterTestHelpers.ApplyToFileName(f, "AbC XyZ"));
        }

        /// <summary>
        /// Verifies capitalize respects skip words.
        /// </summary>
        [Fact]
        public void Apply_Capitalize_SkipsConfiguredWords()
        {
            var f = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.Capitalize, ["a", "the", "for"])
            );
            Assert.Equal("a Song for the World", FilterTestHelpers.ApplyToFileName(f, "a song for the world"));
        }

        /// <summary>
        /// Verifies sentence-case capitalizes after sentence boundaries.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesAfterPunctuation()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            Assert.Equal("Hello world. Next line.", FilterTestHelpers.ApplyToFileName(f, "hello world. next line."));
        }

        /// <summary>
        /// Verifies sentence case capitalizes non-ASCII letters at sentence starts.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesNonAsciiLetters()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            Assert.Equal("École. Über next.", FilterTestHelpers.ApplyToFileName(f, "école. über next."));
        }

        /// <summary>
        /// Verifies sentence case uses configured sentence-end characters.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_UsesConfiguredSentenceEndCharacters()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(new SentenceEndCharactersOptions(Characters: ":;"));
            var lettersFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, [])
            );
            var item = FilterTestHelpers.CreateRenameItem(fileName: "hello: next; again. no");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("Hello: Next; Again. no", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies sentence case with empty sentence-end characters only capitalizes at string start.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_WithNoSentenceEndCharacters_CapitalizesOnlyStart()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(new SentenceEndCharactersOptions(Characters: ""));
            var lettersFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, [])
            );
            var item = FilterTestHelpers.CreateRenameItem(fileName: "hello. next line");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("Hello. next line", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies sentence case ignores sentence-end characters that equal the configured word separator.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_IgnoresSentenceEndCharsMatchingSeparator()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(new SentenceEndCharactersOptions(Characters: ". "));
            var lettersFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, [])
            );
            var item = FilterTestHelpers.CreateRenameItem(fileName: "hello world. next line");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("Hello world. Next line", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies sentence case uses <see cref="RenameItem.WordSeparator"/> after <c>. ! ?</c>, not all Unicode whitespace.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_UsesWordSeparatorAfterPunctuation()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: [])
            );
            var sentenceFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, [])
            );
            var item = FilterTestHelpers.CreateRenameItem(fileName: "hello._world._again");
            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, sentenceFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);
            Assert.Equal("Hello._World._Again", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies capitalize uses the configured word separator and preserves repeated separators.
        /// </summary>
        [Fact]
        public void Apply_Capitalize_UsesWordSeparatorAndPreservesRuns()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: [])
            );
            var capitalizeFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.Capitalize, ["the"])
            );
            var item = FilterTestHelpers.CreateRenameItem(fileName: "__gone__with__the__wind__");

            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, capitalizeFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("__Gone__With__the__Wind__", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies sentence case capitalizes the first letter after leading non-letters.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_SkipsLeadingNonLetters()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            Assert.Equal("03 - Hello. Next", FilterTestHelpers.ApplyToFileName(f, "03 - hello. next"));
        }

        /// <summary>
        /// Verifies sentence case capitalizes after punctuation when one or more separator chars follow.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesAfterPunctuationAndMultipleSeparators()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: [])
            );
            var sentenceFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, [])
            );
            var item = FilterTestHelpers.CreateRenameItem(fileName: "hello.__world!___again?__done");

            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, sentenceFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("Hello.__World!___Again?__Done", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies sentence case does not capitalize after punctuation when separator does not follow.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_DoesNotCapitalizeAfterPunctuationWithoutSeparator()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                _target,
                new SpaceCharacterOptions(SpaceCharacter: '_', Replacements: [])
            );
            var sentenceFilter = new LettersCaseFilter(
                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, [])
            );
            var item = FilterTestHelpers.CreateRenameItem(fileName: "hello.world");

            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, sentenceFilter]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.Equal("Hello.world", item.Preview.FileName);
        }

        /// <summary>
        /// Verifies case inversion.
        /// </summary>
        [Fact]
        public void Apply_InvertCase_SwapsCasing()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.InvertCase, []));
            Assert.Equal("hELLO", FilterTestHelpers.ApplyToFileName(f, "Hello"));
        }

        private static string _BuildUpperMask(string value)
        {
            var chars = new char[value.Length];
            for (var i = 0; i < value.Length; i++)
            {
                chars[i] = char.IsUpper(value[i]) ? 'U' : 'L';
            }

            return new string(chars);
        }
    }
}
