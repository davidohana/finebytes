using Mfr.Filters;
using Mfr.Filters.Replace;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplaceListFilter"/>.
    /// </summary>
    public sealed class ReplaceListFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies that replacements from file are applied in order.
        /// </summary>
        [Fact]
        public void Apply_LiteralPairs_AppliesSequentially()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                \\ replacement list
                a
                b

                .
                _
                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: replaceListFilePath,
                    mode: ReplacerMode.Literal,
                    caseSensitive: true,
                    replaceAll: true,
                    wholeWord: false);

                var result = FilterTestHelpers.ApplyToPrefix(filter, "a.a");

                Assert.Equal("b_b", result);
            }
            finally
            {
                _DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies that an empty replacement strips the search string.
        /// </summary>
        [Fact]
        public void Apply_EmptyReplacementLine_StripsMatchedSearchString()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                x


                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: replaceListFilePath,
                    mode: ReplacerMode.Literal,
                    caseSensitive: true,
                    replaceAll: true,
                    wholeWord: false);

                var result = FilterTestHelpers.ApplyToPrefix(filter, "abxcx");

                Assert.Equal("abc", result);
            }
            finally
            {
                _DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies regex mode and formatter tokens in replacement values.
        /// </summary>
        [Fact]
        public void Apply_RegexAndCounterToken_MatchesPromptExampleBehavior()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                \\ START OF REPLACE LIST
                a
                b

                \.
                _

                [0-9]+
                <counter:10,1,0,2,0>
                \\ END OF REPLACE LIST
                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: replaceListFilePath,
                    mode: ReplacerMode.Regex,
                    caseSensitive: false,
                    replaceAll: true,
                    wholeWord: false);

                var first = FilterTestHelpers.ApplyToPrefix(
                    filter: filter,
                    inputPrefix: "01.-.Blue.Train",
                    globalIndex: 0);
                var second = FilterTestHelpers.ApplyToPrefix(
                    filter: filter,
                    inputPrefix: "02.-.A.Moment's.Notice",
                    globalIndex: 1);

                Assert.Equal("10_-_Blue_Trbin", first);
                Assert.Equal("11_-_b_Moment's_Notice", second);
            }
            finally
            {
                _DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies wildcard mode is supported by replace-list entries.
        /// </summary>
        [Fact]
        public void Apply_WildcardMode_UsesWildcardMatching()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                f*o
                X
                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: replaceListFilePath,
                    mode: ReplacerMode.Wildcard,
                    caseSensitive: true,
                    replaceAll: true,
                    wholeWord: false);

                var result = FilterTestHelpers.ApplyToPrefix(filter, "foo");

                Assert.Equal("X", result);
            }
            finally
            {
                _DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies replace-list file is cached for a chain context and refreshed with a new context.
        /// </summary>
        [Fact]
        public void Apply_ContextCache_ReusesWithinContextAndReloadsAcrossContexts()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                a
                x
                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: replaceListFilePath,
                    mode: ReplacerMode.Literal,
                    caseSensitive: true,
                    replaceAll: true,
                    wholeWord: false);
                var firstChainContext = new FilterChainContext();
                var firstItem = FilterTestHelpers.CreateFile(prefix: "a");
                filter.Apply(firstItem, firstChainContext);
                Assert.Equal("x", firstItem.Preview.Prefix);

                File.WriteAllText(
                    path: replaceListFilePath,
                    contents: "a" + Environment.NewLine + "y" + Environment.NewLine);

                var secondItemSameContext = FilterTestHelpers.CreateFile(prefix: "a");
                filter.Apply(secondItemSameContext, firstChainContext);
                Assert.Equal("x", secondItemSameContext.Preview.Prefix);

                var secondChainContext = new FilterChainContext();
                var thirdItemNewContext = FilterTestHelpers.CreateFile(prefix: "a");
                filter.Apply(thirdItemNewContext, secondChainContext);
                Assert.Equal("y", thirdItemNewContext.Preview.Prefix);
            }
            finally
            {
                _DeleteIfExists(replaceListFilePath);
            }
        }

        private static ReplaceListFilter _CreateFilter(
            string filePath,
            ReplacerMode mode,
            bool caseSensitive,
            bool replaceAll,
            bool wholeWord)
        {
            var options = new ReplaceListOptions(
                FilePath: filePath,
                Mode: mode,
                CaseSensitive: caseSensitive,
                ReplaceAll: replaceAll,
                WholeWord: wholeWord);
            return new ReplaceListFilter(
                Enabled: true,
                Target: _target,
                Options: options);
        }

        private static string _CreateReplaceListFile(string content)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"mfr-replace-list-{Guid.NewGuid():N}.txt");
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
