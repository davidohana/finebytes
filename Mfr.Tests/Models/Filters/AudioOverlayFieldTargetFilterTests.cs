using Mfr.Filters.Formatting;
using Mfr.Filters.Replace;
using Mfr.Models;
using ReplacerFilter = Mfr.Filters.Replace.ReplacerFilter;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests <see cref="AudioOverlayFieldTarget"/> with string-target filters.
    /// </summary>
    public class AudioOverlayFieldTargetFilterTests
    {
        /// <summary>
        /// Verifies formatter output replaces the addressed overlay string field.
        /// </summary>
        [Fact]
        public void Formatter_SetsTitleOnPreviewAudioOverlay()
        {
            var filter = new FormatterFilter(
                new AudioOverlayFieldTarget(AudioOverlayField.Title),
                new FormatterOptions("NextTitle"));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay.Title = "PrevTitle");

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("NextTitle", item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies formatter sets each numeric overlay field from an invariant non-negative integer string.
        /// </summary>
        /// <param name="field">Which <see cref="AudioOverlayField"/> is targeted.</param>
        /// <param name="template">Template output (single integer token).</param>
        /// <param name="expected">Expected <see cref="uint"/> on the overlay.</param>
        [Theory]
        [InlineData(AudioOverlayField.Year, "1999", 1999u)]
        [InlineData(AudioOverlayField.Track, "7", 7u)]
        [InlineData(AudioOverlayField.TrackCount, "12", 12u)]
        [InlineData(AudioOverlayField.Disc, "2", 2u)]
        [InlineData(AudioOverlayField.DiscCount, "3", 3u)]
        public void Formatter_SetsNumericOverlayField(
            AudioOverlayField field,
            string template,
            uint expected)
        {
            var filter = new FormatterFilter(
                new AudioOverlayFieldTarget(field),
                new FormatterOptions(template));
            var item = FilterTestHelpers.CreateRenameItem();

            filter.Setup();
            filter.Apply(item);

            const string nonNumericTheoryMessage = "Theory must only use numeric AudioOverlayField values.";
            var actual = field switch
            {
                AudioOverlayField.Year => item.Preview.AudioTagOverlay.Year,
                AudioOverlayField.Track => item.Preview.AudioTagOverlay.Track,
                AudioOverlayField.TrackCount => item.Preview.AudioTagOverlay.TrackCount,
                AudioOverlayField.Disc => item.Preview.AudioTagOverlay.Disc,
                AudioOverlayField.DiscCount => item.Preview.AudioTagOverlay.DiscCount,
                AudioOverlayField.Title => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Album => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Performers => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.AlbumArtists => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Composers => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Genre => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Comment => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Lyrics => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Copyright => throw new InvalidOperationException(nonNumericTheoryMessage),
                AudioOverlayField.Grouping => throw new InvalidOperationException(nonNumericTheoryMessage),
                _ => throw new InvalidOperationException(nonNumericTheoryMessage),
            };

            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Verifies an empty formatter template clears a previously set numeric overlay field.
        /// </summary>
        [Fact]
        public void Formatter_EmptyTemplate_ClearsNumericOverlayField()
        {
            var filter = new FormatterFilter(
                new AudioOverlayFieldTarget(AudioOverlayField.Year),
                new FormatterOptions(string.Empty));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay.Year = 2001);

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Year);
        }

        /// <summary>
        /// Verifies whitespace-only template clears numeric overlay (same as empty).
        /// </summary>
        [Fact]
        public void Formatter_WhitespaceOnlyTemplate_ClearsNumericOverlayField()
        {
            var filter = new FormatterFilter(
                new AudioOverlayFieldTarget(AudioOverlayField.Track),
                new FormatterOptions("   "));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay.Track = 9);

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Track);
        }

        /// <summary>
        /// Verifies non-integer template text throws when assigning a numeric overlay field.
        /// </summary>
        [Fact]
        public void Formatter_InvalidNumericTemplate_ThrowsArgumentException()
        {
            var filter = new FormatterFilter(
                new AudioOverlayFieldTarget(AudioOverlayField.Disc),
                new FormatterOptions("not-a-number"));

            var item = FilterTestHelpers.CreateRenameItem();
            filter.Setup();

            var ex = Assert.Throws<ArgumentException>(() => filter.Apply(item));
            Assert.Contains("non-negative integer", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies replacer can change a numeric field expressed as digit strings.
        /// </summary>
        [Fact]
        public void Replacer_ReplacesNumericYearStringOnPreviewAudioOverlay()
        {
            var filter = new ReplacerFilter(
                new AudioOverlayFieldTarget(AudioOverlayField.Year),
                new ReplacerOptions(
                    Find: "199",
                    Replacement: "200",
                    Mode: ReplacerMode.Literal,
                    CaseSensitive: true,
                    ReplaceAll: false,
                    WholeWord: false));

            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.AudioTagOverlay.Year = 1999;
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(2009u, item.Preview.AudioTagOverlay.Year);
        }

        /// <summary>
        /// Verifies replacer mutates the addressed overlay field.
        /// </summary>
        [Fact]
        public void Replacer_ReplacesGenreOnPreviewAudioOverlay()
        {
            var filter = new ReplacerFilter(
                new AudioOverlayFieldTarget(AudioOverlayField.Genre),
                new ReplacerOptions(
                    Find: "Rock",
                    Replacement: "Metal",
                    Mode: ReplacerMode.Literal,
                    CaseSensitive: true,
                    ReplaceAll: false,
                    WholeWord: false));

            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay.Genre = "Hard Rock");

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Hard Metal", item.Preview.AudioTagOverlay.Genre);
        }

        /// <summary>
        /// Verifies directory rows cannot hydrate tags for overlay targets.
        /// </summary>
        [Fact]
        public void Apply_ToDirectory_ThrowsInvalidOperation()
        {
            var filter = new FormatterFilter(
                new AudioOverlayFieldTarget(AudioOverlayField.Title),
                new FormatterOptions("x"));

            var item = FilterTestHelpers.CreateRenameItem(
                attributes: FileAttributes.Directory,
                extension: string.Empty);

            filter.Setup();

            var ex = Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
