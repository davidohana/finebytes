using Mfr.Filters.Formatting.Tokens.GeneralGroup;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.GeneralGroup
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
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 4);

            Assert.Equal("5", token.Resolve(arg: "", item: item));
        }
    }
}
