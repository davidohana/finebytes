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
        /// Verifies bare-arg default matches <c>&lt;counter:1,1,0,2,0&gt;</c> (no leading zeros).
        /// </summary>
        [Fact]
        public void Resolve_EmptyArg_DefaultsToLegacySimpleForm()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 0);

            Assert.Equal("1", token.Compile(arg: "")(item));
            Assert.Equal("1", token.Compile(arg: "1,1,0,2,0")(item));
        }

        /// <summary>
        /// Verifies reset-on-folder uses in-folder index when set to <c>1</c>.
        /// </summary>
        [Fact]
        public void Resolve_ResetOnFolderOne_UsesInFolderIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 2, inFolderIndex: 7);

            Assert.Equal("17", token.Compile(arg: "10,1,0,2,1")(item));
        }

        /// <summary>
        /// Verifies reset off uses global index.
        /// </summary>
        [Fact]
        public void Resolve_ResetOff_UsesGlobalIndex()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 2, inFolderIndex: 7);

            Assert.Equal("12", token.Compile(arg: "10,1,0,2,0")(item));
        }

        /// <summary>
        /// Verifies custom leading-zero mode pads with zeros to the requested length.
        /// </summary>
        [Fact]
        public void Resolve_LeadingZeroesModeCustom_PadsWithZeros()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 0, inFolderIndex: 0);

            Assert.Equal("0007", token.Compile(arg: "7,1,2,4,0")(item));
        }

        /// <summary>
        /// Verifies leading-zeroes mode none ignores the fourth parameter.
        /// </summary>
        [Fact]
        public void Resolve_LeadingZeroesModeNone_SkipsPadding()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 5, inFolderIndex: 0);

            Assert.Equal("15", token.Compile(arg: "10,1,0,99,0")(item));
        }

        /// <summary>
        /// Verifies automatic mode derives width from global list size.
        /// </summary>
        [Fact]
        public void Resolve_LeadingZeroesModeAutomaticGlobal_UnifiesWidth()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(
                renameListIndex: 5,
                renameListTotalCount: 100,
                renameListFolderSiblingCount: 3);

            Assert.Equal("006", token.Compile(arg: "1,1,1,2,0")(item));
        }

        /// <summary>
        /// Verifies automatic mode with per-folder reset uses folder-local counts.
        /// </summary>
        [Fact]
        public void Resolve_LeadingZeroesModeAutomaticPerFolder_UnifiesWidthWithinFolder()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(
                renameListIndex: 50,
                inFolderIndex: 5,
                renameListTotalCount: 100,
                renameListFolderSiblingCount: 100);

            Assert.Equal("006", token.Compile(arg: "1,1,1,2,1")(item));
        }

        /// <summary>
        /// Verifies a negative step decrements as expected (mode none).
        /// </summary>
        [Fact]
        public void Resolve_NegativeStep_Decrements()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem(renameListIndex: 3, inFolderIndex: 0);

            Assert.Equal("4", token.Compile(arg: "10,-2,0,0,0")(item));
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
                () => token.Compile(arg: "1,1,1,2,0")(item));
            Assert.Contains("automatic leading-zero mode", ex.Message);
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
                () => token.Compile(arg: "1,1,2,0,0")(item));
            Assert.Contains("positive total length", ex.Message);
        }

        /// <summary>
        /// Verifies unknown leading-zeroes-mode throws.
        /// </summary>
        [Fact]
        public void Resolve_InvalidLeadingZeroesMode_Throws()
        {
            var token = new CounterToken();
            var item = FilterTestHelpers.CreateRenameItem();

            var ex = Assert.Throws<ArgumentException>(
                () => token.Compile(arg: "1,1,9,2,0")(item));
            Assert.Contains("leading-zeroes-mode", ex.Message);
        }
    }
}
