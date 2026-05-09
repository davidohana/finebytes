using Mfr.Filters.Formatting;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="NameListFilter"/>.
    /// </summary>
    public sealed class NameListFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies list line N maps to global index N.
        /// </summary>
        [Fact]
        public void Apply_MapsLineIndexToGlobalIndex()
        {
            var path = _WriteTemp(
                """
                Alpha
                Beta
                Gamma
                """);
            try
            {
                var f = _CreateFilter(path);
                Assert.Equal("Alpha", FilterTestHelpers.ApplyToPrefix(f, "old0", renameListIndex: 0));
                Assert.Equal("Beta", FilterTestHelpers.ApplyToPrefix(f, "old1", renameListIndex: 1));
                Assert.Equal("Gamma", FilterTestHelpers.ApplyToPrefix(f, "old2", renameListIndex: 2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies prefix and suffix templates resolve with formatter tokens.
        /// </summary>
        [Fact]
        public void Apply_PrefixSuffixAndCounterToken()
        {
            var path = _WriteTemp("One");
            try
            {
                var f = new NameListFilter(
                    Target: _target,
                    Options: new NameListOptions(
                        FilePath: path,
                        Prefix: "<counter:10,1,none,2,global>_",
                        Suffix: "_end"));
                Assert.Equal("10_One_end", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 0));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies blank lines are preserved as entries.
        /// </summary>
        [Fact]
        public void Apply_BlankLines_AreEntries()
        {
            var path = _WriteTemp(
                """
                First

                Second
                """);
            try
            {
                var f = _CreateFilter(path);
                Assert.Equal("First", FilterTestHelpers.ApplyToPrefix(f, "a", renameListIndex: 0));
                Assert.Equal(string.Empty, FilterTestHelpers.ApplyToPrefix(f, "b", renameListIndex: 1));
                Assert.Equal("Second", FilterTestHelpers.ApplyToPrefix(f, "c", renameListIndex: 2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies blank-line entries still participate in index mapping.
        /// </summary>
        [Fact]
        public void Apply_BlankLineMapping_IncludesEmptyEntries()
        {
            var path = _WriteTemp(
                """
                A

                B
                """);
            try
            {
                var f = _CreateFilter(path);
                Assert.Equal("A", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 0));
                Assert.Equal(string.Empty, FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 1));
                Assert.Equal("B", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies out-of-range index throws <see cref="UserException"/>.
        /// </summary>
        [Fact]
        public void Apply_TooFewLines_ThrowsUserException()
        {
            var path = _WriteTemp("Only");
            try
            {
                var f = _CreateFilter(path);
                var ex = Assert.Throws<UserException>(() =>
                    FilterTestHelpers.ApplyToPrefix(f, "old", renameListIndex: 1));
                Assert.Contains("Name-list has", ex.Message, StringComparison.Ordinal);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies comment lines do not consume list indices.
        /// </summary>
        [Fact]
        public void Apply_CommentLinesSkipped()
        {
            var path = _WriteTemp(
                """
                // header
                Real1
                # also a comment
                Real2
                """);
            try
            {
                var f = _CreateFilter(path);
                Assert.Equal("Real1", FilterTestHelpers.ApplyToPrefix(f, "a", renameListIndex: 0));
                Assert.Equal("Real2", FilterTestHelpers.ApplyToPrefix(f, "b", renameListIndex: 1));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static NameListFilter _CreateFilter(string path)
        {
            return new NameListFilter(
                Target: _target,
                Options: new NameListOptions(
                    FilePath: path,
                    Prefix: "",
                    Suffix: ""));
        }

        private static string _WriteTemp(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), $"mfr_namelist_tests_{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, content);
            return path;
        }
    }
}
