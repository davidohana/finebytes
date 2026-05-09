using Mfr.Filters.Formatting.Tokens.Session;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Session
{
    /// <summary>
    /// Tests for <see cref="CounterToken"/>.
    /// </summary>
    public sealed class CounterTokenTests
    {
        /// <summary>
        /// Verifies bare-arg default matches <c>&lt;counter:1,1,none,2,global&gt;</c> (no leading zeros).
        /// </summary>
        [Fact]
        public void Resolve_EmptyArg_DefaultsToSimpleForm()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 0);

            Assert.Equal("1", token.Compile(arg: "")(item));
            Assert.Equal("1", token.Compile(arg: "1,1,none,2,global")(item));
        }

        /// <summary>
        /// Verifies per-folder reset uses in-folder index when set to <c>perFolder</c>.
        /// </summary>
        [Fact]
        public void Resolve_ResetPerFolder_UsesInFolderIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 2, inFolderIndex: 7);

            Assert.Equal("17", token.Compile(arg: "10,1,none,2,perFolder")(item));
        }

        /// <summary>
        /// Verifies global reset uses global index.
        /// </summary>
        [Fact]
        public void Resolve_ResetGlobal_UsesGlobalIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 2, inFolderIndex: 7);

            Assert.Equal("12", token.Compile(arg: "10,1,none,2,global")(item));
        }

        /// <summary>
        /// Verifies fixed padding pads with zeros to the requested length.
        /// </summary>
        [Fact]
        public void Resolve_PaddingFixed_PadsWithZeros()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 0, inFolderIndex: 0);

            Assert.Equal("0007", token.Compile(arg: "7,1,fixed,4,global")(item));
        }

        /// <summary>
        /// Verifies padding none ignores the fourth parameter.
        /// </summary>
        [Fact]
        public void Resolve_PaddingNone_SkipsPadding()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 5, inFolderIndex: 0);

            Assert.Equal("15", token.Compile(arg: "10,1,none,99,global")(item));
        }

        /// <summary>
        /// Verifies automatic padding derives width from global list size.
        /// </summary>
        [Fact]
        public void Resolve_PaddingAutomaticGlobal_UnifiesWidth()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(
                renameListIndex: 5,
                renameListTotalCount: 100,
                renameListFolderSiblingCount: 3);

            Assert.Equal("006", token.Compile(arg: "1,1,auto,2,global")(item));
        }

        /// <summary>
        /// Verifies automatic padding with per-folder reset uses folder-local counts.
        /// </summary>
        [Fact]
        public void Resolve_PaddingAutomaticPerFolder_UnifiesWidthWithinFolder()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(
                renameListIndex: 50,
                inFolderIndex: 5,
                renameListTotalCount: 100,
                renameListFolderSiblingCount: 100);

            Assert.Equal("006", token.Compile(arg: "1,1,auto,2,perFolder")(item));
        }

        /// <summary>
        /// Verifies a negative step decrements as expected with padding none.
        /// </summary>
        [Fact]
        public void Resolve_NegativeStep_Decrements()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 3, inFolderIndex: 0);

            Assert.Equal("4", token.Compile(arg: "10,-2,none,0,global")(item));
        }

        /// <summary>
        /// Verifies fewer than five comma-separated arguments throws.
        /// </summary>
        [Fact]
        public void Resolve_TooFewArgs_Throws()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(
                () => token.Compile(arg: "1,2")(item));
            Assert.Contains("Invalid <counter> token arg", ex.Message);
        }

        /// <summary>
        /// Verifies automatic mode throws when list totals were not populated.
        /// </summary>
        [Fact]
        public void Resolve_AutomaticWithoutListCounts_Throws()
        {
            var token = new CounterToken();
            var item = new RenameItem(new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: @"C:\Music\Album",
                prefix: "x",
                extension: ".mp3",
                renameListTotalCount: 0,
                renameListFolderSiblingCount: 0));

            var ex = Assert.Throws<InvalidOperationException>(
                () => token.Compile(arg: "1,1,auto,2,global")(item));
            Assert.Contains("automatic padding", ex.Message);
        }

        /// <summary>
        /// Verifies custom mode rejects non-positive length.
        /// </summary>
        [Fact]
        public void Resolve_CustomModeNonPositiveLength_Throws()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(
                () => token.Compile(arg: "1,1,fixed,0,global")(item));
            Assert.Contains("positive total length", ex.Message);
        }

        /// <summary>
        /// Verifies unknown padding keyword throws.
        /// </summary>
        [Fact]
        public void Resolve_InvalidPadding_Throws()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(
                () => token.Compile(arg: "1,1,nope,2,global")(item));
            Assert.Contains("padding", ex.Message);
        }
    }
}
