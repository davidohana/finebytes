using Mfr.App.Ui.Views.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatTokenColorizingTransformer"/> highlight range math.
    /// </summary>
    public sealed class FormatTokenColorizingTransformerTests
    {
        /// <summary>
        /// Verifies an alias shorter than its canonical name colors only the written name, not past <c>&gt;</c>.
        /// </summary>
        [Fact]
        public void TryGetColorRanges_AliasShorterThanCanonical_UsesWrittenNameLength()
        {
            var span = new FormatTokenSpan(
                Start: 10,
                Length: "<ext>".Length,
                CanonicalName: "file-extension",
                WrittenName: "ext",
                Args: ""
            );

            Assert.True(
                FormatTokenColorizingTransformer.TryGetColorRanges(
                    span,
                    out var openStart,
                    out var openEnd,
                    out var nameStart,
                    out var nameEnd,
                    out var closeStart,
                    out var closeEnd
                )
            );

            Assert.Equal(10, openStart);
            Assert.Equal(11, openEnd);
            Assert.Equal(11, nameStart);
            Assert.Equal(14, nameEnd);
            Assert.Equal(14, closeStart);
            Assert.Equal(15, closeEnd);
            Assert.True(nameEnd <= closeStart);
            Assert.Equal(span.WrittenName.Length, nameEnd - nameStart);
            Assert.NotEqual(span.CanonicalName.Length, nameEnd - nameStart);
        }

        /// <summary>
        /// Verifies name-only tokens color the name up to (not including) the closing delimiter.
        /// </summary>
        [Fact]
        public void TryGetColorRanges_NameOnly_NameEndsAtCloseDelimiter()
        {
            var span = new FormatTokenSpan(
                Start: 0,
                Length: "<file-name>".Length,
                CanonicalName: "file-name",
                WrittenName: "file-name",
                Args: ""
            );

            Assert.True(
                FormatTokenColorizingTransformer.TryGetColorRanges(
                    span,
                    out _,
                    out _,
                    out var nameStart,
                    out var nameEnd,
                    out var closeStart,
                    out _
                )
            );

            Assert.Equal(1, nameStart);
            Assert.Equal(closeStart, nameEnd);
            Assert.Equal("file-name".Length, nameEnd - nameStart);
        }

        /// <summary>
        /// Verifies args (including the colon) stay outside the name range.
        /// </summary>
        [Fact]
        public void TryGetColorRanges_WithArgs_NameStopsBeforeColon()
        {
            var text = "<counter:initial=1>";
            var span = new FormatTokenSpan(
                Start: 0,
                Length: text.Length,
                CanonicalName: "counter",
                WrittenName: "counter",
                Args: "initial=1"
            );

            Assert.True(
                FormatTokenColorizingTransformer.TryGetColorRanges(
                    span,
                    out _,
                    out _,
                    out var nameStart,
                    out var nameEnd,
                    out var closeStart,
                    out var closeEnd
                )
            );

            Assert.Equal(1, nameStart);
            Assert.Equal(1 + "counter".Length, nameEnd);
            Assert.Equal(':', text[nameEnd]);
            Assert.Equal(text.Length - 1, closeStart);
            Assert.Equal(text.Length, closeEnd);
            Assert.True(nameEnd < closeStart);
        }

        /// <summary>
        /// Verifies nested <c>&lt;…&gt;</c> inside args do not extend the outer name range.
        /// </summary>
        [Fact]
        public void TryGetColorRanges_NestedArgs_OuterNameOnly()
        {
            var text = "<substr:start=1,source=<file-name>>";
            var span = new FormatTokenSpan(
                Start: 0,
                Length: text.Length,
                CanonicalName: "substr",
                WrittenName: "substr",
                Args: "start=1,source=<file-name>"
            );

            Assert.True(
                FormatTokenColorizingTransformer.TryGetColorRanges(
                    span,
                    out _,
                    out _,
                    out var nameStart,
                    out var nameEnd,
                    out var closeStart,
                    out _
                )
            );

            Assert.Equal(1, nameStart);
            Assert.Equal(1 + "substr".Length, nameEnd);
            Assert.Equal(text.Length - 1, closeStart);
            Assert.Contains("<file-name>", text[nameEnd..closeStart], StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies an empty written name yields a zero-width name range (delimiters only).
        /// </summary>
        [Fact]
        public void TryGetColorRanges_EmptyWrittenName_ZeroWidthName()
        {
            var span = new FormatTokenSpan(Start: 0, Length: 2, CanonicalName: "x", WrittenName: "", Args: "");

            Assert.True(
                FormatTokenColorizingTransformer.TryGetColorRanges(
                    span,
                    out var openStart,
                    out var openEnd,
                    out var nameStart,
                    out var nameEnd,
                    out var closeStart,
                    out var closeEnd
                )
            );

            Assert.Equal(0, openStart);
            Assert.Equal(1, openEnd);
            Assert.Equal(1, nameStart);
            Assert.Equal(1, nameEnd);
            Assert.Equal(1, closeStart);
            Assert.Equal(2, closeEnd);
        }

        /// <summary>
        /// Verifies a too-long WrittenName is clamped so it never paints into <c>&gt;</c>.
        /// </summary>
        [Fact]
        public void TryGetColorRanges_OverlongWrittenName_ClampedBeforeClose()
        {
            var span = new FormatTokenSpan(
                Start: 0,
                Length: "<ext>".Length,
                CanonicalName: "file-extension",
                WrittenName: "file-extension",
                Args: ""
            );

            Assert.True(
                FormatTokenColorizingTransformer.TryGetColorRanges(
                    span,
                    out _,
                    out _,
                    out var nameStart,
                    out var nameEnd,
                    out var closeStart,
                    out _
                )
            );

            Assert.Equal(1, nameStart);
            Assert.Equal(closeStart, nameEnd);
            Assert.True(nameEnd - nameStart < span.WrittenName.Length);
        }

        /// <summary>
        /// Verifies spans shorter than <c>&lt;&gt;</c> are rejected.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void TryGetColorRanges_TooShort_ReturnsFalse(int length)
        {
            var span = new FormatTokenSpan(Start: 0, Length: length, CanonicalName: "x", WrittenName: "x", Args: "");

            Assert.False(
                FormatTokenColorizingTransformer.TryGetColorRanges(span, out _, out _, out _, out _, out _, out _)
            );
        }
    }
}
