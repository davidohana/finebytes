using Mfr.Models;
using Mfr.Models.Filters.Advanced;

namespace Mfr.Tests.Models.Filters.Advanced
{
    /// <summary>
    /// Tests for <see cref="CounterFilter"/>.
    /// </summary>
    public class CounterFilterTests
    {
        private static readonly FileNameTarget _Target = new(FileNameTargetMode.Prefix);

        /// <summary>
        /// Verifies replace mode outputs padded counter only.
        /// </summary>
        [Fact]
        public void Apply_Replace_ReturnsFormattedCounter()
        {
            var f = new CounterFilter(
                true,
                _Target,
                new CounterOptions(Start: 1, Step: 1, Width: 3, PadChar: "0", Position: CounterPosition.Replace, Separator: "", ResetPerFolder: false));
            var file = FilterTestHelpers.CreateFile(globalIndex: 4);
            Assert.Equal("005", f.Apply("old", file));
        }

        /// <summary>
        /// Verifies prepend mode.
        /// </summary>
        [Fact]
        public void Apply_Prepend_PrefixesCounter()
        {
            var f = new CounterFilter(
                true,
                _Target,
                new CounterOptions(Start: 0, Step: 1, Width: 0, PadChar: "0", Position: CounterPosition.Prepend, Separator: "_", ResetPerFolder: false));
            var file = FilterTestHelpers.CreateFile(globalIndex: 2);
            Assert.Equal("2_name", f.Apply("name", file));
        }

        /// <summary>
        /// Verifies append mode.
        /// </summary>
        [Fact]
        public void Apply_Append_AppendsCounter()
        {
            var f = new CounterFilter(
                true,
                _Target,
                new CounterOptions(Start: 0, Step: 1, Width: 0, PadChar: "0", Position: CounterPosition.Append, Separator: "-", ResetPerFolder: false));
            var file = FilterTestHelpers.CreateFile(globalIndex: 1);
            Assert.Equal("name-1", f.Apply("name", file));
        }

        /// <summary>
        /// Verifies folder occurrence index when reset per folder is enabled.
        /// </summary>
        [Fact]
        public void Apply_ResetPerFolder_UsesFolderOccurrenceIndex()
        {
            var f = new CounterFilter(
                true,
                _Target,
                new CounterOptions(Start: 10, Step: 5, Width: 0, PadChar: "0", Position: CounterPosition.Replace, Separator: "", ResetPerFolder: true));
            var file = FilterTestHelpers.CreateFile(globalIndex: 99, folderOccurrenceIndex: 2);
            Assert.Equal("20", f.Apply("x", file));
        }
    }
}
