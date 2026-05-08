using Mfr.Filters.Formatting.Tokens.GeneralGroup;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.GeneralGroup
{
    /// <summary>
    /// Tests for <see cref="CounterToken"/>.
    /// </summary>
    public sealed class CounterTokenTests
    {
        /// <summary>
        /// Verifies the counter uses the global index when reset is <c>0</c>.
        /// </summary>
        [Fact]
        public void Resolve_ResetZero_UsesGlobalIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 2, inFolderIndex: 7);

            Assert.Equal("12", token.Resolve(arg: "10,1,0,2,0", item: item));
        }

        /// <summary>
        /// Verifies the counter uses the in-folder index when reset is <c>1</c>.
        /// </summary>
        [Fact]
        public void Resolve_ResetOne_UsesInFolderIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 2, inFolderIndex: 7);

            Assert.Equal("17", token.Resolve(arg: "10,1,1,2,0", item: item));
        }

        /// <summary>
        /// Verifies pad mode <c>1</c> pads with spaces.
        /// </summary>
        [Fact]
        public void Resolve_PadModeSpace_PadsWithSpaces()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 0, inFolderIndex: 0);

            Assert.Equal("   7", token.Resolve(arg: "7,1,0,4,1", item: item));
        }

        /// <summary>
        /// Verifies pad mode <c>0</c> pads with zeros.
        /// </summary>
        [Fact]
        public void Resolve_PadModeZero_PadsWithZeros()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 0, inFolderIndex: 0);

            Assert.Equal("0007", token.Resolve(arg: "7,1,0,4,0", item: item));
        }

        /// <summary>
        /// Verifies a width of <c>0</c> emits the raw value with no padding.
        /// </summary>
        [Fact]
        public void Resolve_ZeroWidth_NoPadding()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 5, inFolderIndex: 0);

            Assert.Equal("15", token.Resolve(arg: "10,1,0,0,0", item: item));
        }

        /// <summary>
        /// Verifies a negative step decrements as expected.
        /// </summary>
        [Fact]
        public void Resolve_NegativeStep_Decrements()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(globalIndex: 3, inFolderIndex: 0);

            Assert.Equal("4", token.Resolve(arg: "10,-2,0,0,0", item: item));
        }

        /// <summary>
        /// Verifies fewer than five comma-separated arguments throws.
        /// </summary>
        [Fact]
        public void Resolve_TooFewArgs_Throws()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<InvalidOperationException>(
                () => token.Resolve(arg: "1,2", item: item));
            Assert.Contains("Invalid counter token arg", ex.Message);
        }
    }
}
