using Mfr.Core;
using Mfr.Filters.Replace;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplaceListParser"/>.
    /// </summary>
    public sealed class ReplaceListParserTests
    {
        /// <summary>
        /// Verifies parser accepts comments with '//'.
        /// </summary>
        [Fact]
        public void ParseFile_DoubleSlashComments_AreIgnored()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                // replacement list
                a
                b
                // separator

                // next pair
                .
                _
                """);
            try
            {
                var entries = ReplaceListParser.ParseFile(replaceListFilePath);

                Assert.Equal(2, entries.Count);
                Assert.Equal("a", entries[0].Search);
                Assert.Equal("b", entries[0].Replacement);
                Assert.Equal(".", entries[1].Search);
                Assert.Equal("_", entries[1].Replacement);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser accepts comments with '#'.
        /// </summary>
        [Fact]
        public void ParseFile_HashComments_AreIgnored()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                # replacement list
                a
                b
                # separator

                # next pair
                .
                _
                """);
            try
            {
                var entries = ReplaceListParser.ParseFile(replaceListFilePath);

                Assert.Equal(2, entries.Count);
                Assert.Equal("a", entries[0].Search);
                Assert.Equal("b", entries[0].Replacement);
                Assert.Equal(".", entries[1].Search);
                Assert.Equal("_", entries[1].Replacement);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser accepts comments with '\\'.
        /// </summary>
        [Fact]
        public void ParseFile_BackslashComments_AreIgnored()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                \\ replacement list
                a
                b
                \\ separator

                \\ next pair
                .
                _
                """);
            try
            {
                var entries = ReplaceListParser.ParseFile(replaceListFilePath);

                Assert.Equal(2, entries.Count);
                Assert.Equal("a", entries[0].Search);
                Assert.Equal("b", entries[0].Replacement);
                Assert.Equal(".", entries[1].Search);
                Assert.Equal("_", entries[1].Replacement);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies '#'-prefixed text without following space is treated as data, not comment.
        /// </summary>
        [Fact]
        public void ParseFile_HashWithoutSpace_IsNotComment()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                #a
                b
                """);
            try
            {
                var entries = ReplaceListParser.ParseFile(replaceListFilePath);

                Assert.Single(entries);
                Assert.Equal("#a", entries[0].Search);
                Assert.Equal("b", entries[0].Replacement);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser rejects empty replacement lines.
        /// </summary>
        [Fact]
        public void ParseFile_EmptyReplacementLine_Throws()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                x


                """);
            try
            {
                var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(replaceListFilePath));
                Assert.Contains("replace line cannot be empty", ex.Message);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser rejects empty search lines.
        /// </summary>
        [Fact]
        public void ParseFile_EmptySearchLine_Throws()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """

                a
                b
                """);
            try
            {
                var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(replaceListFilePath));
                Assert.Contains("search line cannot be empty", ex.Message);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser maps &lt;EMPTY&gt; token to empty replacement.
        /// </summary>
        [Fact]
        public void ParseFile_EmptyReplacementToken_MapsToEmptyString()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                x
                <EMPTY>

                """);
            try
            {
                var entries = ReplaceListParser.ParseFile(replaceListFilePath);

                Assert.Single(entries);
                Assert.Equal("x", entries[0].Search);
                Assert.Equal("", entries[0].Replacement);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser rejects dangling search without replacement.
        /// </summary>
        [Fact]
        public void ParseFile_SearchWithoutReplacement_Throws()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                a
                """);
            try
            {
                var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(replaceListFilePath));
                Assert.Contains("without a corresponding replace line", ex.Message);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser enforces blank separator line between pairs.
        /// </summary>
        [Fact]
        public void ParseFile_MissingBlankLineBetweenPairs_Throws()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                a
                b
                c
                d
                """);
            try
            {
                var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(replaceListFilePath));
                Assert.Contains("expected exactly one empty separator line between entry pairs", ex.Message);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser rejects more than one blank separator line between pairs.
        /// </summary>
        [Fact]
        public void ParseFile_MultipleBlankLinesBetweenPairs_Throws()
        {
            var replaceListFilePath = CreateReplaceListFile(
                """
                a
                b


                c
                d
                """);
            try
            {
                var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(replaceListFilePath));
                Assert.Contains("expected exactly one empty separator line between entry pairs", ex.Message);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser rejects search lines over maximum length.
        /// </summary>
        [Fact]
        public void ParseFile_SearchLineLongerThan1000_Throws()
        {
            var tooLongSearch = new string('a', 1001);
            var replaceListFilePath = CreateReplaceListFile(
                $"{tooLongSearch}{Environment.NewLine}b{Environment.NewLine}");
            try
            {
                var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(replaceListFilePath));
                Assert.Contains("line length exceeds 1000", ex.Message);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        /// <summary>
        /// Verifies parser rejects replace lines over maximum length.
        /// </summary>
        [Fact]
        public void ParseFile_ReplaceLineLongerThan1000_Throws()
        {
            var tooLongReplace = new string('b', 1001);
            var replaceListFilePath = CreateReplaceListFile(
                $"a{Environment.NewLine}{tooLongReplace}{Environment.NewLine}");
            try
            {
                var ex = Assert.Throws<UserException>(() => ReplaceListParser.ParseFile(replaceListFilePath));
                Assert.Contains("line length exceeds 1000", ex.Message);
            }
            finally
            {
                DeleteIfExists(replaceListFilePath);
            }
        }

        private static string CreateReplaceListFile(string content)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"mfr-replace-list-parser-{Guid.NewGuid():N}.txt");
            File.WriteAllText(tempFilePath, content.ReplaceLineEndings(Environment.NewLine));
            return tempFilePath;
        }

        private static void DeleteIfExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            File.Delete(filePath);
        }
    }
}
