using Mfr.Filters.Formatting;

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
            var f = _CreateFilter(["Alpha", "Beta", "Gamma"]);
            Assert.Equal("Alpha", FilterTestHelpers.ApplyToPrefix(f, "old0", renameListIndex: 0));
            Assert.Equal("Beta", FilterTestHelpers.ApplyToPrefix(f, "old1", renameListIndex: 1));
            Assert.Equal("Gamma", FilterTestHelpers.ApplyToPrefix(f, "old2", renameListIndex: 2));
        }

        /// <summary>
        /// Verifies prefix and suffix templates resolve with formatter tokens.
        /// </summary>
        [Fact]
        public void Apply_PrefixSuffixAndCounterToken()
        {
            var f = new NameListFilter(
                Target: _target,
                Options: new NameListOptions(
                    Entries: ["One"],
                    Prefix: "<counter:initial=10,step=1,padding=none,length=2,resetScope=global>_",
                    Suffix: "_end"
                )
            );
            Assert.Equal("10_One_end", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 0));
        }

        /// <summary>
        /// Verifies blank lines are preserved as entries.
        /// </summary>
        [Fact]
        public void Apply_BlankLines_AreEntries()
        {
            var f = _CreateFilter(["First", "", "Second"]);
            Assert.Equal("First", FilterTestHelpers.ApplyToPrefix(f, "a", renameListIndex: 0));
            Assert.Equal(string.Empty, FilterTestHelpers.ApplyToPrefix(f, "b", renameListIndex: 1));
            Assert.Equal("Second", FilterTestHelpers.ApplyToPrefix(f, "c", renameListIndex: 2));
        }

        /// <summary>
        /// Verifies blank-line entries still participate in index mapping.
        /// </summary>
        [Fact]
        public void Apply_BlankLineMapping_IncludesEmptyEntries()
        {
            var f = _CreateFilter(["A", "", "B"]);
            Assert.Equal("A", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 0));
            Assert.Equal(string.Empty, FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 1));
            Assert.Equal("B", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 2));
        }

        /// <summary>
        /// Verifies an empty list leaves the original value unchanged.
        /// </summary>
        [Fact]
        public void Apply_EmptyList_IsNoOp()
        {
            var f = _CreateFilter([]);
            Assert.Equal("old", FilterTestHelpers.ApplyToPrefix(f, "old", renameListIndex: 0));
        }

        /// <summary>
        /// Verifies out-of-range index throws <see cref="UserException"/>.
        /// </summary>
        [Fact]
        public void Apply_TooFewLines_ThrowsUserException()
        {
            var f = _CreateFilter(["Only"]);
            var ex = Assert.Throws<UserException>(() => FilterTestHelpers.ApplyToPrefix(f, "old", renameListIndex: 1));
            Assert.Equal(
                "Name-list has 1 line(s) but rename item is 2. Add lines or adjust the rename list.",
                ex.Message
            );
        }

        /// <summary>
        /// Verifies comment-like lines are kept as names in the embedded list.
        /// </summary>
        [Fact]
        public void Apply_CommentLikeLines_AreNames()
        {
            var f = _CreateFilter(["// header", "Real1"]);
            Assert.Equal("// header", FilterTestHelpers.ApplyToPrefix(f, "a", renameListIndex: 0));
            Assert.Equal("Real1", FilterTestHelpers.ApplyToPrefix(f, "b", renameListIndex: 1));
        }

        private static NameListFilter _CreateFilter(IReadOnlyList<string> entries)
        {
            return new NameListFilter(
                Target: _target,
                Options: new NameListOptions(Entries: entries, Prefix: "", Suffix: "")
            );
        }
    }
}
