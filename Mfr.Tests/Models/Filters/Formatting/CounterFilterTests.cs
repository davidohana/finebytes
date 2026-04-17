using Mfr.Filters.Formatting;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="CounterFilter"/>.
    /// </summary>
    public class CounterFilterTests
    {
        private static readonly FileNameTarget _target = new(FileNamePart.Prefix);

        /// <summary>
        /// Verifies replace mode outputs padded counter only.
        /// </summary>
        [Fact]
        public void Apply_Replace_ReturnsFormattedCounter()
        {
            var f = new CounterFilter(
                true,
                _target,
                new CounterOptions(Start: 1, Step: 1, Width: 3, PadChar: "0", Position: CounterPosition.Replace, Separator: "", ResetPerFolder: false));
            Assert.Equal("005", FilterTestHelpers.ApplyToPrefix(f, "old", globalIndex: 4));
        }

        /// <summary>
        /// Verifies prepend mode.
        /// </summary>
        [Fact]
        public void Apply_Prepend_PrefixesCounter()
        {
            var f = new CounterFilter(
                true,
                _target,
                new CounterOptions(Start: 0, Step: 1, Width: 0, PadChar: "0", Position: CounterPosition.Prepend, Separator: "_", ResetPerFolder: false));
            Assert.Equal("2_name", FilterTestHelpers.ApplyToPrefix(f, "name", globalIndex: 2));
        }

        /// <summary>
        /// Verifies append mode.
        /// </summary>
        [Fact]
        public void Apply_Append_AppendsCounter()
        {
            var f = new CounterFilter(
                true,
                _target,
                new CounterOptions(Start: 0, Step: 1, Width: 0, PadChar: "0", Position: CounterPosition.Append, Separator: "-", ResetPerFolder: false));
            Assert.Equal("name-1", FilterTestHelpers.ApplyToPrefix(f, "name", globalIndex: 1));
        }

        /// <summary>
        /// Verifies in-folder index when reset per folder is enabled.
        /// </summary>
        [Fact]
        public void Apply_ResetPerFolder_UsesInFolderIndex()
        {
            var f = new CounterFilter(
                true,
                _target,
                new CounterOptions(Start: 10, Step: 5, Width: 0, PadChar: "0", Position: CounterPosition.Replace, Separator: "", ResetPerFolder: true));
            Assert.Equal("20", FilterTestHelpers.ApplyToPrefix(f, "x", globalIndex: 99, inFolderIndex: 2));
        }

        /// <summary>
        /// Verifies <c>padChar</c> <c>"1"</c> pads with space (documented in filter guide).
        /// </summary>
        [Fact]
        public void Apply_PadCharSpace_PadsWithSpaces()
        {
            var f = new CounterFilter(
                true,
                _target,
                new CounterOptions(Start: 7, Step: 1, Width: 4, PadChar: "1", Position: CounterPosition.Replace, Separator: "", ResetPerFolder: false));
            Assert.Equal("   7", FilterTestHelpers.ApplyToPrefix(f, "x", globalIndex: 0));
        }
    }
}
