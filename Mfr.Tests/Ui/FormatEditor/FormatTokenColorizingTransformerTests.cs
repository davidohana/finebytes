using Mfr.App.Ui.Views.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatTokenColorizingTransformer"/> name-range and number-range geometry.
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

        /// <summary>
        /// Verifies positive and negative numeric literals in args are ranged.
        /// </summary>
        [Fact]
        public void EnumerateNumberRanges_CounterArgs_FindsPositiveAndNegative()
        {
            var text = "<substr:start=1,end=-1,source=x>";
            var span = _Span(text, "substr", "start=1,end=-1,source=x");

            var ranges = FormatTokenColorizingTransformer.EnumerateNumberRanges(span).ToList();

            Assert.Equal(2, ranges.Count);
            Assert.Equal("1", _Slice(text, ranges[0]));
            Assert.Equal("-1", _Slice(text, ranges[1]));
        }

        /// <summary>
        /// Verifies nested token names with digits are skipped while nested-arg numbers are kept.
        /// </summary>
        [Fact]
        public void EnumerateNumberRanges_NestedToken_SkipsNameDigits_KeepsNestedArgNumbers()
        {
            var text = "<substr:start=2,end=5,source=<id3v2:TXXX,0>>";
            var span = _Span(text, "substr", "start=2,end=5,source=<id3v2:TXXX,0>");

            var ranges = FormatTokenColorizingTransformer.EnumerateNumberRanges(span).ToList();
            var slices = ranges.Select(r => _Slice(text, r)).ToList();

            Assert.Equal(["2", "5", "0"], slices);
            Assert.DoesNotContain("3", slices);
        }

        /// <summary>
        /// Verifies empty args yield no number ranges.
        /// </summary>
        [Fact]
        public void EnumerateNumberRanges_NoArgs_Empty()
        {
            var span = new FormatTokenSpan(
                Start: 0,
                Length: 11,
                CanonicalName: "file-name",
                WrittenName: "file-name",
                Args: ""
            );

            Assert.Empty(FormatTokenColorizingTransformer.EnumerateNumberRanges(span));
        }

        private static FormatTokenSpan _Span(string text, string writtenName, string args)
        {
            return new FormatTokenSpan(
                Start: 0,
                Length: text.Length,
                CanonicalName: writtenName,
                WrittenName: writtenName,
                Args: args
            );
        }

        private static string _Slice(string text, (int Start, int End) range)
        {
            return text[range.Start..range.End];
        }
    }
}
