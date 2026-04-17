using Mfr.Filters.Case;
using Mfr.Filters;
using Mfr.Filters.Space;
using Mfr.Core;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Case
{
    /// <summary>
    /// Tests for <see cref="CasingListFilter"/>.
    /// </summary>
    public sealed class CasingListFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies casing-list words are applied and unknown words remain unchanged.
        /// </summary>
        [Fact]
        public void Apply_UsesCasingListAndLeavesUnknownWordsUnchanged()
        {
            var casingListFilePath = _CreateCasingListFile(
                """
                and
                or
                with
                RMX
                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: casingListFilePath,
                    uppercaseSentenceInitial: false,
                    sentenceEndChars: ".!?");

                var result = FilterTestHelpers.ApplyToPrefix(filter, "03 - WiTH Or Without You Rmx");

                Assert.Equal("03 - with or Without You RMX", result);
            }
            finally
            {
                _DeleteIfExists(casingListFilePath);
            }
        }

        /// <summary>
        /// Verifies sentence-initial uppercase and custom sentence-end characters.
        /// </summary>
        [Fact]
        public void Apply_WithUppercaseSentenceInitial_UppercasesAfterSentenceBoundaries()
        {
            var casingListFilePath = _CreateCasingListFile(
                """
                and
                or
                with
                RMX
                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: casingListFilePath,
                    uppercaseSentenceInitial: true,
                    sentenceEndChars: "-.!");

                var result = FilterTestHelpers.ApplyToPrefix(filter, "03 - WiTH Or Without You Rmx");

                Assert.Equal("03 - With or Without You RMX", result);
            }
            finally
            {
                _DeleteIfExists(casingListFilePath);
            }
        }

        /// <summary>
        /// Verifies configured word separator from SpaceCharacter is respected.
        /// </summary>
        [Fact]
        public void Apply_AfterSpaceCharacter_UsesConfiguredWordSeparator()
        {
            var casingListFilePath = _CreateCasingListFile(
                """
                and
                us
                them
                """);
            try
            {
                var spaceCharacterFilter = new SpaceCharacterFilter(
                    Enabled: true,
                    Target: _target,
                    Options: new SpaceCharacterOptions(
                        SpaceCharacter: '_',
                        ReplaceSpaces: true,
                        ReplaceUnderscores: false,
                        ReplacePercent20: false,
                        CustomText: ""));
                var casingFilter = _CreateFilter(
                    filePath: casingListFilePath,
                    uppercaseSentenceInitial: true,
                    sentenceEndChars: ".!?");
                var filters = new List<BaseFilter> { spaceCharacterFilter, casingFilter };

                var item = FilterTestHelpers.CreateRenameItem(prefix: "US_AND_THEM");
                filters.SetupFilters();
                item.ApplyFilters(filters);

                Assert.Equal("Us_and_them", item.Preview.Prefix);
            }
            finally
            {
                _DeleteIfExists(casingListFilePath);
            }
        }

        /// <summary>
        /// Verifies setup fails when casing-list path is invalid.
        /// </summary>
        [Fact]
        public void Setup_MissingFile_ThrowsUserException()
        {
            var missingFilePath = Path.Combine(Path.GetTempPath(), $"mfr-casing-list-missing-{Guid.NewGuid():N}.txt");
            var filter = _CreateFilter(
                filePath: missingFilePath,
                uppercaseSentenceInitial: false,
                sentenceEndChars: ".!?");

            var ex = Assert.Throws<UserException>(filter.Setup);
            Assert.Contains("Casing-list file not found", ex.Message, StringComparison.Ordinal);
        }

        private static CasingListFilter _CreateFilter(
            string filePath,
            bool uppercaseSentenceInitial,
            string sentenceEndChars)
        {
            var options = new CasingListOptions(
                FilePath: filePath,
                UppercaseSentenceInitial: uppercaseSentenceInitial,
                SentenceEndChars: sentenceEndChars);
            return new CasingListFilter(
                Enabled: true,
                Target: _target,
                Options: options);
        }

        private static string _CreateCasingListFile(string content)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"mfr-casing-list-{Guid.NewGuid():N}.txt");
            File.WriteAllText(tempFilePath, content.ReplaceLineEndings(Environment.NewLine));
            return tempFilePath;
        }

        private static void _DeleteIfExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            File.Delete(filePath);
        }
    }
}
