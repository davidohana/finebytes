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
            var f = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.UpperCase, []));
            Assert.Equal("HELLO", FilterTestHelpers.ApplyToPrefix(f, "hello"));
        }

        /// <summary>
        /// Verifies lower-case mode.
        /// </summary>
        [Fact]
        public void Apply_LowerCase_ConvertsToLowerInvariant()
        {
            var f = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.LowerCase, []));
            Assert.Equal("hello", FilterTestHelpers.ApplyToPrefix(f, "HELLO"));
        }

        /// <summary>
        /// Verifies first-letter-up mode uppercases the first letter and lowercases remaining letters.
        /// </summary>
        [Fact]
        public void Apply_FirstLetterUp_UppercasesFirstLetterOnly()
        {
            var f = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.FirstLetterUp, []));
            Assert.Equal("Hello world", FilterTestHelpers.ApplyToPrefix(f, "hELLO world"));
        }

        /// <summary>
        /// Verifies first-letter-up mode applies to index 0 and lowercases the remainder.
        /// </summary>
        [Fact]
        public void Apply_FirstLetterUp_UsesIndexZero()
        {
            var f = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.FirstLetterUp, []));
            Assert.Equal(" 123_abc", FilterTestHelpers.ApplyToPrefix(f, " 123_aBC"));
        }

        /// <summary>
        /// Verifies title-case respects skip words.
        /// </summary>
        [Fact]
        public void Apply_TitleCase_SkipsConfiguredWords()
        {
            var f = new LettersCaseFilter(
                true,
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
            var f = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            Assert.Equal("Hello world. Next line.", FilterTestHelpers.ApplyToPrefix(f, "hello world. next line."));
        }

        /// <summary>
        /// Verifies sentence case uses <see cref="RenameItem.WordSeparator"/> after <c>. ! ?</c>, not all Unicode whitespace.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_UsesWordSeparatorAfterPunctuation()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var sentenceFilter = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var file = FilterTestHelpers.CreateFile(prefix: "hello._world._again");
            file.ApplyFilters([spaceCharFilter, sentenceFilter]);
            Assert.Equal("Hello._World._Again", file.Preview.Prefix);
        }

        /// <summary>
        /// Verifies title case uses the configured word separator and preserves repeated separators.
        /// </summary>
        [Fact]
        public void Apply_TitleCase_UsesWordSeparatorAndPreservesRuns()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var titleFilter = new LettersCaseFilter(
                true,
                _target,
                new LettersCaseOptions(LettersCaseMode.TitleCase, ["the"]));
            var file = FilterTestHelpers.CreateFile(prefix: "__gone__with__the__wind__");

            file.ApplyFilters([spaceCharFilter, titleFilter]);

            Assert.Equal("__Gone__With__the__Wind__", file.Preview.Prefix);
        }

        /// <summary>
        /// Verifies sentence case capitalizes after punctuation when one or more separator chars follow.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_CapitalizesAfterPunctuationAndMultipleSeparators()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var sentenceFilter = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var file = FilterTestHelpers.CreateFile(prefix: "hello.__world!___again?__done");

            file.ApplyFilters([spaceCharFilter, sentenceFilter]);

            Assert.Equal("Hello.__World!___Again?__Done", file.Preview.Prefix);
        }

        /// <summary>
        /// Verifies sentence case does not capitalize after punctuation when separator does not follow.
        /// </summary>
        [Fact]
        public void Apply_SentenceCase_DoesNotCapitalizeAfterPunctuationWithoutSeparator()
        {
            var spaceCharFilter = new SpaceCharacterFilter(
                true,
                _target,
                new SpaceCharacterOptions(
                    SpaceCharacter: '_',
                    ReplaceSpaces: false,
                    ReplaceUnderscores: false,
                    ReplacePercent20: false,
                    CustomText: ""));
            var sentenceFilter = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.SentenceCase, []));
            var file = FilterTestHelpers.CreateFile(prefix: "hello.world");

            file.ApplyFilters([spaceCharFilter, sentenceFilter]);

            Assert.Equal("Hello.world", file.Preview.Prefix);
        }

        /// <summary>
        /// Verifies case inversion.
        /// </summary>
        [Fact]
        public void Apply_InvertCase_SwapsCasing()
        {
            var f = new LettersCaseFilter(true, _target, new LettersCaseOptions(LettersCaseMode.InvertCase, []));
            Assert.Equal("hELLO", FilterTestHelpers.ApplyToPrefix(f, "Hello"));
        }
    }
}
