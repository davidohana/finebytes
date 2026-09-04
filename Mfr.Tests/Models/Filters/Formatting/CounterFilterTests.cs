using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="CounterFilter"/>.
    /// </summary>
    public class CounterFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies replace mode outputs padded counter only.
        /// </summary>
        [Fact]
        public void Apply_Replace_ReturnsFormattedCounter()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: 1,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.Custom,
                    CustomLength: 3,
                    Position: CounterPosition.Replace,
                    Separator: "",
                    ResetPerFolder: false
                )
            );
            Assert.Equal("005", FilterTestHelpers.ApplyToPrefix(f, "old", renameListIndex: 4));
        }

        /// <summary>
        /// Verifies prepend mode.
        /// </summary>
        [Fact]
        public void Apply_Prepend_PrefixesCounter()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: 0,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.None,
                    CustomLength: 2,
                    Position: CounterPosition.Prepend,
                    Separator: "_",
                    ResetPerFolder: false
                )
            );
            Assert.Equal("2_name", FilterTestHelpers.ApplyToPrefix(f, "name", renameListIndex: 2));
        }

        /// <summary>
        /// Verifies append mode.
        /// </summary>
        [Fact]
        public void Apply_Append_AppendsCounter()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: 0,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.None,
                    CustomLength: 2,
                    Position: CounterPosition.Append,
                    Separator: "-",
                    ResetPerFolder: false
                )
            );
            Assert.Equal("name-1", FilterTestHelpers.ApplyToPrefix(f, "name", renameListIndex: 1));
        }

        /// <summary>
        /// Verifies in-folder index when reset per folder is enabled.
        /// </summary>
        [Fact]
        public void Apply_ResetPerFolder_UsesInFolderIndex()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: 10,
                    Step: 5,
                    LeadingZerosMode: CounterLeadingZerosMode.None,
                    CustomLength: 2,
                    Position: CounterPosition.Replace,
                    Separator: "",
                    ResetPerFolder: true
                )
            );
            Assert.Equal("20", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 99, inFolderIndex: 2));
        }

        /// <summary>
        /// Verifies custom padding places the sign before zero digits (not <c>PadLeft</c> mangling).
        /// </summary>
        [Fact]
        public void Apply_Custom_Negative_PadsSignSafe()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: -5,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.Custom,
                    CustomLength: 3,
                    Position: CounterPosition.Replace,
                    Separator: "",
                    ResetPerFolder: false
                )
            );
            Assert.Equal("-005", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 0));
        }

        /// <summary>
        /// Verifies automatic padding uses rename-list total count.
        /// </summary>
        [Fact]
        public void Apply_Automatic_PadsToListWidth()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: 1,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.Automatic,
                    CustomLength: 2,
                    Position: CounterPosition.Replace,
                    Separator: "",
                    ResetPerFolder: false
                )
            );

            // List of 100 → indices 0..99 → values 1..100 → width 3
            var first = FilterTestHelpers.CreateRenameItem(prefix: "x", renameListIndex: 0, renameListTotalCount: 100);
            f.Setup();
            f.Apply(first);
            Assert.Equal("001", first.Preview.Prefix);

            var last = FilterTestHelpers.CreateRenameItem(prefix: "x", renameListIndex: 99, renameListTotalCount: 100);
            f.Setup();
            f.Apply(last);
            Assert.Equal("100", last.Preview.Prefix);
        }

        /// <summary>
        /// Verifies automatic padding with reset-per-folder uses folder sibling count.
        /// </summary>
        [Fact]
        public void Apply_Automatic_ResetPerFolder_UsesFolderSiblingWidth()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: 1,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.Automatic,
                    CustomLength: 2,
                    Position: CounterPosition.Replace,
                    Separator: "",
                    ResetPerFolder: true
                )
            );

            // Global list 1000 would need width 4; folder of 10 → values 1..10 → width 2
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "x",
                renameListIndex: 50,
                inFolderIndex: 0,
                renameListTotalCount: 1000,
                renameListFolderSiblingCount: 10
            );
            f.Setup();
            f.Apply(item);
            Assert.Equal("01", item.Preview.Prefix);
        }

        /// <summary>
        /// Verifies automatic width uses absolute digit count so negatives pad like MFR7.
        /// </summary>
        [Fact]
        public void Apply_Automatic_NegativeRange_PadsSignSafe()
        {
            var f = new CounterFilter(
                _target,
                new CounterOptions(
                    Start: -9,
                    Step: 1,
                    LeadingZerosMode: CounterLeadingZerosMode.Automatic,
                    CustomLength: 2,
                    Position: CounterPosition.Replace,
                    Separator: "",
                    ResetPerFolder: false
                )
            );

            // Indices 0..9 → values -9..0 → digit width 1; index 0 → "-9"
            var item = FilterTestHelpers.CreateRenameItem(prefix: "x", renameListIndex: 0, renameListTotalCount: 10);
            f.Setup();
            f.Apply(item);
            Assert.Equal("-9", item.Preview.Prefix);
        }
    }
}
