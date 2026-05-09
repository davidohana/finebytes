using Mfr.Filters.Formatting.Tokens.General;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Tests for <see cref="ItemCountToken"/>.
    /// </summary>
    public sealed class ItemCountTokenTests
    {
        /// <summary>
        /// Verifies the token returns the rename-list total from metadata.
        /// </summary>
        [Fact]
        public void Resolve_UsesRenameListTotalCount()
        {
            var token = new ItemCountToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListTotalCount: 42);

            Assert.Equal("42", token.Resolve(arg: "", item: item));
        }

        /// <summary>
        /// Verifies unresolved metadata defaults to helper-derived totals.
        /// </summary>
        [Fact]
        public void Resolve_DefaultHelperTotals_MatchesDerivedListLength()
        {
            var token = new ItemCountToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 4);

            Assert.Equal("5", token.Resolve(arg: "", item: item));
        }

        /// <summary>
        /// Verifies stray arguments are rejected.
        /// </summary>
        [Fact]
        public void Resolve_WithArgument_Throws()
        {
            var token = new ItemCountToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<InvalidOperationException>(() => token.Resolve(arg: "1", item: item));
            Assert.Contains("item-count", ex.Message);
        }
    }
}
