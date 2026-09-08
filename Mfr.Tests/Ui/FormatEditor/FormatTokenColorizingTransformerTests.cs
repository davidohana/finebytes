using Mfr.App.Ui.Views.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatTokenColorizingTransformer"/> name-range geometry.
    /// </summary>
    public sealed class FormatTokenColorizingTransformerTests
    {
        /// <summary>
        /// Verifies alias written length (not canonical) drives the name range.
        /// </summary>
        [Fact]
        public void TryGetNameRange_AliasShorterThanCanonical_UsesWrittenNameLength()
        {
            // "<ext>" — WrittenName "ext", CanonicalName would be longer ("file-extension").
            var span = new FormatTokenSpan(
                Start: 0,
                Length: 5,
                CanonicalName: "file-extension",
                WrittenName: "ext",
                Args: ""
            );

            Assert.True(FormatTokenColorizingTransformer.TryGetNameRange(span, out var nameStart, out var nameEnd));
            Assert.Equal(1, nameStart);
            Assert.Equal(4, nameEnd);
        }

        /// <summary>
        /// Verifies args after <c>:</c> are excluded from the name range.
        /// </summary>
        [Fact]
        public void TryGetNameRange_WithArgs_NameStopsBeforeColon()
        {
            var text = "<counter:initial=1>";
            var span = new FormatTokenSpan(
                Start: 0,
                Length: text.Length,
                CanonicalName: "counter",
                WrittenName: "counter",
                Args: "initial=1"
            );

            Assert.True(FormatTokenColorizingTransformer.TryGetNameRange(span, out var nameStart, out var nameEnd));
            Assert.Equal(1, nameStart);
            Assert.Equal(1 + "counter".Length, nameEnd);
            Assert.Equal(':', text[nameEnd]);
        }

        /// <summary>
        /// Verifies an overlong WrittenName is clamped before <c>&gt;</c>.
        /// </summary>
        [Fact]
        public void TryGetNameRange_OverlongWrittenName_ClampedBeforeClose()
        {
            var span = new FormatTokenSpan(
                Start: 0,
                Length: 5,
                CanonicalName: "file-extension",
                WrittenName: "file-extension",
                Args: ""
            );

            Assert.True(FormatTokenColorizingTransformer.TryGetNameRange(span, out var nameStart, out var nameEnd));
            Assert.Equal(1, nameStart);
            Assert.Equal(4, nameEnd);
        }
    }
}
