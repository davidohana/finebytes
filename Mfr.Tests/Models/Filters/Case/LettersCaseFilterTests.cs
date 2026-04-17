using Mfr.Core;
using Mfr.Filters.Case;
using Mfr.Filters.Space;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="LettersCaseFilter"/> transformations.
    /// </summary>
    public class LettersCaseFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies upper-case mode.
        /// </summary>
        [Fact]
        public void Apply_UpperCase_ConvertsToUpperInvariant()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.UpperCase, []));
            Assert.Equal("HELLO", FilterTestHelpers.ApplyToPrefix(f, "hello"));
        }

        /// <summary>
        /// Verifies lower-case mode.
        /// </summary>
        [Fact]
        public void Apply_LowerCase_ConvertsToLowerInvariant()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.LowerCase, []));
            Assert.Equal("hello", FilterTestHelpers.ApplyToPrefix(f, "HELLO"));
        }

        /// <summary>
        /// Verifies first-letter-up mode uppercases the first letter and lowercases remaining letters.
        /// </summary>
        [Fact]
        public void Apply_FirstLetterUp_UppercasesFirstLetterOnly()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.FirstLetterUp, []));
            Assert.Equal("Hello world", FilterTestHelpers.ApplyToPrefix(f, "hELLO world"));
        }

        /// <summary>
        /// Verifies first-letter-up mode applies to index 0 and lowercases the remainder.
        /// </summary>
        [Fact]
        public void Apply_FirstLetterUp_UsesIndexZero()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.FirstLetterUp, []));
            Assert.Equal(" 123_abc", FilterTestHelpers.ApplyToPrefix(f, " 123_aBC"));
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
                    SkipWords: [],
                    WeirdUppercaseChancePercent: 0,
                    WeirdFixedPlaces: false));
            Assert.Equal("abc xyz", FilterTestHelpers.ApplyToPrefix(f, "AbC XyZ"));
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
                    SkipWords: [],
                    WeirdUppercaseChancePercent: 100,
                    WeirdFixedPlaces: false));
            Assert.Equal("ABC XYZ", FilterTestHelpers.ApplyToPrefix(f, "AbC XyZ"));
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
                    SkipWords: [],
                    WeirdUppercaseChancePercent: 50,
                    WeirdFixedPlaces: true));
            var a = FilterTestHelpers.ApplyToPrefix(f, "abcdefgh", globalIndex: 0);
            var b = FilterTestHelpers.ApplyToPrefix(f, "qrstuvwx", globalIndex: 999);

            Assert.Equal(
                _BuildUpperMask(a),
                _BuildUpperMask(b));
        }

        /// <summary>
        /// Verifies title-case respects skip words.
        /// </summary>
        [Fact]
        public void Apply_TitleCase_SkipsConfiguredWords()
        {
            var f = new LettersCaseFilter(
                                _target,
                new LettersCaseOptions(LettersCaseMode.TitleCase, ["a", "the", "for"]));
            Assert.Equal("a Song for the World", FilterTestHelpers.ApplyToPrefix(f, "a song for the world"));
        }

        /// <summary>
        /// Verifies sentence-case capitalizes after sentence boundaries.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesAfterPunctuation()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            Assert.Equal("Hello world. Next line.", FilterTestHelpers.ApplyToPrefix(f, "hello world. next line."));
        }

        /// <summary>
        /// Verifies sentence case uses configured sentence-end characters.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_UsesConfiguredSentenceEndCharacters()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(
                                _target,
                new SentenceEndCharactersOptions(Characters: ":;"));
            var lettersFilter = new LettersCaseFilter(
                                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "hello: next; again. no");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);

            Assert.Equal("Hello: Next; Again. no", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies sentence case with empty sentence-end characters only capitalizes at string start.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_WithNoSentenceEndCharacters_CapitalizesOnlyStart()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(
                                _target,
                new SentenceEndCharactersOptions(Characters: ""));
            var lettersFilter = new LettersCaseFilter(
                                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "hello. next line");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);

            Assert.Equal("Hello. next line", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies sentence case ignores sentence-end characters that equal the configured word separator.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_IgnoresSentenceEndCharsMatchingSeparator()
        {
            var sentenceEndFilter = new SentenceEndCharactersFilter(
                                _target,
                new SentenceEndCharactersOptions(Characters: ". "));
            var lettersFilter = new LettersCaseFilter(
                                _target,
                new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "hello world. next line");
            var chain = FilterChain.CreateAllEnabled([sentenceEndFilter, lettersFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);

            Assert.Equal("Hello world. Next line", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies sentence case uses <see cref="RenameItem.WordSeparator"/> after <c>. ! ?</c>, not all Unicode whitespace.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_UsesWordSeparatorAfterPunctuation()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var sentenceFilter = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "hello._world._again");
            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, sentenceFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);
            Assert.Equal("Hello._World._Again", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies title case uses the configured word separator and preserves repeated separators.
        /// </summary>
        [Fact]
        public void Apply_TitleCase_UsesWordSeparatorAndPreservesRuns()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var titleFilter = new LettersCaseFilter(
                                _target,
                new LettersCaseOptions(LettersCaseMode.TitleCase, ["the"]));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "__gone__with__the__wind__");

            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, titleFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);

            Assert.Equal("__Gone__With__the__Wind__", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies sentence case capitalizes after punctuation when one or more separator chars follow.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesAfterPunctuationAndMultipleSeparators()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var sentenceFilter = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "hello.__world!___again?__done");

            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, sentenceFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);

            Assert.Equal("Hello.__World!___Again?__Done", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies sentence case does not capitalize after punctuation when separator does not follow.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_DoesNotCapitalizeAfterPunctuationWithoutSeparator()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var sentenceFilter = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var item = FilterTestHelpers.CreateRenameItem(prefix: "hello.world");

            var chain = FilterChain.CreateAllEnabled([spaceCharFilter, sentenceFilter]);
            chain.SetupFilters();
            item.ApplyFilters(chain);

            Assert.Equal("Hello.world", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies case inversion.
        /// </summary>
        [Fact]
        public void Apply_InvertCase_SwapsCasing()
        {
            var f = new LettersCaseFilter(_target, new LettersCaseOptions(LettersCaseMode.InvertCase, []));
            Assert.Equal("hELLO", FilterTestHelpers.ApplyToPrefix(f, "Hello"));
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
