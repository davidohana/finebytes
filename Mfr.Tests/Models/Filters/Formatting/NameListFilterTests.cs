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
                var f = _CreateFilter(path, skipEmptyLines: false);
                Assert.Equal("Alpha", FilterTestHelpers.ApplyToPrefix(f, "old0", globalIndex: 0));
                Assert.Equal("Beta", FilterTestHelpers.ApplyToPrefix(f, "old1", globalIndex: 1));
                Assert.Equal("Gamma", FilterTestHelpers.ApplyToPrefix(f, "old2", globalIndex: 2));
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
                        SkipEmptyLines: true,
                        Prefix: "<counter:10,1,0,2,0>_",
                        Suffix: "_end"));
                Assert.Equal("10_One_end", FilterTestHelpers.ApplyToPrefix(f, "x", globalIndex: 0));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies blank lines are omitted from the mapping when <c>SkipEmptyLines</c> is true.
        /// </summary>
        [Fact]
        public void Apply_SkipEmptyLines_IgnoresBlankLines()
        {
            var path = _WriteTemp(
                """
                First


                Second
                """);
            try
            {
                var f = _CreateFilter(path, skipEmptyLines: true);
                Assert.Equal("First", FilterTestHelpers.ApplyToPrefix(f, "a", globalIndex: 0));
                Assert.Equal("Second", FilterTestHelpers.ApplyToPrefix(f, "b", globalIndex: 1));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies blank lines become empty entries when <c>SkipEmptyLines</c> is false.
        /// </summary>
        [Fact]
        public void Apply_DoNotSkipEmptyLines_IncludesEmptyEntries()
        {
            var path = _WriteTemp(
                """
                A

                B
                """);
            try
            {
                var f = _CreateFilter(path, skipEmptyLines: false);
                Assert.Equal("A", FilterTestHelpers.ApplyToPrefix(f, "x", globalIndex: 0));
                Assert.Equal(string.Empty, FilterTestHelpers.ApplyToPrefix(f, "x", globalIndex: 1));
                Assert.Equal("B", FilterTestHelpers.ApplyToPrefix(f, "x", globalIndex: 2));
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
                var f = _CreateFilter(path, skipEmptyLines: true);
                var ex = Assert.Throws<UserException>(() =>
                    FilterTestHelpers.ApplyToPrefix(f, "old", globalIndex: 1));
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
                var f = _CreateFilter(path, skipEmptyLines: false);
                Assert.Equal("Real1", FilterTestHelpers.ApplyToPrefix(f, "a", globalIndex: 0));
                Assert.Equal("Real2", FilterTestHelpers.ApplyToPrefix(f, "b", globalIndex: 1));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static NameListFilter _CreateFilter(string path, bool skipEmptyLines)
        {
            return new NameListFilter(
                Target: _target,
                Options: new NameListOptions(
                    FilePath: path,
                    SkipEmptyLines: skipEmptyLines,
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
