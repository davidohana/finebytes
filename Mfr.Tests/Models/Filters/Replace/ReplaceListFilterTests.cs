using Mfr.Filters.Replace;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplaceListFilter"/>.
    /// </summary>
    public sealed class ReplaceListFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies that replacements from file are applied in order.
        /// </summary>
        [Fact]
        public void Apply_LiteralPairs_AppliesSequentially()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                // replacement list
                S:a
                R:b

                S:.
                R:_
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
        /// Verifies that the &lt;EMPTY&gt; token strips the search string.
        /// </summary>
        [Fact]
        public void Apply_EmptyReplacementToken_StripsMatchedSearchString()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                S:x
                R:<EMPTY>

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
                // START OF REPLACE LIST
                S:a
                R:b

                S:\.
                R:_

                S:[0-9]+
                R:<counter:10,1,0,2,0>
                // END OF REPLACE LIST
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
                S:f*o
                R:X
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
        /// Verifies replace-list file is cached for the filter instance lifetime.
        /// </summary>
        [Fact]
        public void Apply_InstanceCache_ReusesAcrossApplyCalls()
        {
            var replaceListFilePath = _CreateReplaceListFile(
                """
                S:a
                R:x
                """);
            try
            {
                var filter = _CreateFilter(
                    filePath: replaceListFilePath,
                    mode: ReplacerMode.Literal,
                    caseSensitive: true,
                    replaceAll: true,
                    wholeWord: false);
                filter.Setup();
                var firstItem = FilterTestHelpers.CreateRenameItem(prefix: "a");
                filter.Apply(firstItem);
                Assert.Equal("x", firstItem.Preview.Prefix);

                File.WriteAllText(
                    path: replaceListFilePath,
                    contents: "S:a" + Environment.NewLine + "R:y" + Environment.NewLine);

                var secondItem = FilterTestHelpers.CreateRenameItem(prefix: "a");
                filter.Apply(secondItem);
                Assert.Equal("x", secondItem.Preview.Prefix);
            }
            finally
            {
                _DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies setup fails when file path is invalid.
        /// </summary>
        [Fact]
        public void Setup_MissingFile_ThrowsUserException()
        {
            var missingFilePath = Path.Combine(Path.GetTempPath(), $"mfr-replace-list-missing-{Guid.NewGuid():N}.txt");
            var filter = _CreateFilter(
                filePath: missingFilePath,
                mode: ReplacerMode.Literal,
                caseSensitive: true,
                replaceAll: true,
                wholeWord: false);

            var ex = Assert.Throws<UserException>(filter.Setup);
            Assert.Contains("Replace-list file not found", ex.Message, StringComparison.Ordinal);
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
