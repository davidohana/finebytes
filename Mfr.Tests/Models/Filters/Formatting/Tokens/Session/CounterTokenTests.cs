using Mfr.Filters.Formatting.Tokens.Session;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Session
{
    /// <summary>
    /// Tests for <see cref="CounterToken"/>.
    /// </summary>
    public sealed class CounterTokenTests
    {
        private const string _defaultsEquivalent =
            "initial=1,step=1,padding=none,length=2,resetScope=global";

        /// <summary>
        /// Verifies bare-arg default matches encoded defaults (no leading zeros).
        /// </summary>
        [Fact]
        public void Resolve_EmptyArg_DefaultsToSimpleForm()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 0);

            Assert.Equal("1", token.Compile(tokenArgs: "")(item));
            Assert.Equal("1", token.Compile(tokenArgs: _defaultsEquivalent)(item));
        }

        /// <summary>
        /// Verifies named options can appear in any order.
        /// </summary>
        [Fact]
        public void Resolve_NamedOptions_OrderIndependent()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 0);

            Assert.Equal(
                "1",
                token.Compile(tokenArgs: "resetScope=global,length=2,padding=none,step=1,initial=1")(item));
        }

        /// <summary>
        /// Verifies per-folder reset uses in-folder index when set to <c>perFolder</c>.
        /// </summary>
        [Fact]
        public void Resolve_ResetPerFolder_UsesInFolderIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 2, inFolderIndex: 7);

            Assert.Equal(
                "17",
                token.Compile(tokenArgs: "initial=10,step=1,padding=none,length=2,resetScope=perFolder")(item));
        }

        /// <summary>
        /// Verifies global reset uses global index.
        /// </summary>
        [Fact]
        public void Resolve_ResetGlobal_UsesGlobalIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 2, inFolderIndex: 7);

            Assert.Equal(
                "12",
                token.Compile(tokenArgs: "initial=10,step=1,padding=none,length=2,resetScope=global")(item));
        }

        /// <summary>
        /// Verifies fixed padding pads with zeros to the requested length.
        /// </summary>
        [Fact]
        public void Resolve_PaddingFixed_PadsWithZeros()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 0, inFolderIndex: 0);

            Assert.Equal(
                "0007",
                token.Compile(tokenArgs: "initial=7,step=1,padding=fixed,length=4,resetScope=global")(item));
        }

        /// <summary>
        /// Verifies padding none ignores length.
        /// </summary>
        [Fact]
        public void Resolve_PaddingNone_SkipsPadding()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 5, inFolderIndex: 0);

            Assert.Equal(
                "15",
                token.Compile(tokenArgs: "initial=10,step=1,padding=none,length=99,resetScope=global")(item));
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

            Assert.Equal(
                "006",
                token.Compile(tokenArgs: "initial=1,step=1,padding=auto,length=2,resetScope=global")(item));
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

            Assert.Equal(
                "006",
                token.Compile(tokenArgs: "initial=1,step=1,padding=auto,length=2,resetScope=perFolder")(item));
        }

        /// <summary>
        /// Verifies a negative step decrements as expected with padding none.
        /// </summary>
        [Fact]
        public void Resolve_NegativeStep_Decrements()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 3, inFolderIndex: 0);

            Assert.Equal(
                "4",
                token.Compile(tokenArgs: "initial=10,step=-2,padding=none,length=0,resetScope=global")(item));
        }

        /// <summary>
        /// Verifies positional-style fragments without <c>=</c> throw.
        /// </summary>
        [Fact]
        public void Resolve_PositionalStyleArgs_Throws()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(
                () => token.Compile(tokenArgs: "1,2")(item));
            Assert.Contains("<counter>", ex.Message);
            Assert.Contains("not a valid name=value pair", ex.Message);
        }

        /// <summary>
        /// Verifies automatic mode throws when list totals were not populated.
        /// </summary>
        [Fact]
        public void Resolve_AutomaticWithoutListCounts_Throws()
        {
            var token = new CounterToken();
            var item = new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: @"C:\Music\Album",
                    prefix: "x",
                    extension: ".mp3",
                    renameListTotalCount: 0,
                    renameListFolderSiblingCount: 0));

            var ex = Assert.Throws<InvalidOperationException>(
                () => token.Compile(tokenArgs: "initial=1,step=1,padding=auto,length=2,resetScope=global")(item));
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
                () => token.Compile(tokenArgs: "initial=1,step=1,padding=fixed,length=0,resetScope=global")(item));
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
                () => token.Compile(tokenArgs: "initial=1,step=1,padding=nope,length=2,resetScope=global")(item));
            Assert.Contains("padding", ex.Message);
        }
    }
}
