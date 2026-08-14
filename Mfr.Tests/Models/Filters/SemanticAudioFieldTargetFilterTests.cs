using Mfr.Filters.Formatting;
using Mfr.Filters.Replace;
using Mfr.Models.Tags;
using ReplacerFilter = Mfr.Filters.Replace.ReplacerFilter;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests <see cref="SemanticAudioFieldTarget"/> with string-target filters.
    /// </summary>
    public class SemanticAudioFieldTargetFilterTests
    {
        /// <summary>
        /// Verifies formatter output replaces the addressed overlay string field.
        /// </summary>
        [Fact]
        public void Formatter_SetsTitleOnPreviewAudioOverlay()
        {
            var filter = new FormatterFilter(
                new SemanticAudioFieldTarget(SemanticAudioField.Title),
                new FormatterOptions("NextTitle"));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "PrevTitle"));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("NextTitle", item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies formatter sets each numeric overlay field from a non-negative integer decimal string.
        /// </summary>
        /// <param name="field">Which <see cref="SemanticAudioField"/> is targeted.</param>
        /// <param name="template">Template output (single integer token).</param>
        /// <param name="expected">Expected <see cref="uint"/> on the overlay.</param>
        [Theory]
        [InlineData(SemanticAudioField.Year, "1999", 1999u)]
        [InlineData(SemanticAudioField.Track, "7", 7u)]
        [InlineData(SemanticAudioField.TrackCount, "12", 12u)]
        [InlineData(SemanticAudioField.Disc, "2", 2u)]
        [InlineData(SemanticAudioField.DiscCount, "3", 3u)]
        public void Formatter_SetsNumericOverlayField(
            SemanticAudioField field,
            string template,
            uint expected)
        {
            var filter = new FormatterFilter(
                new SemanticAudioFieldTarget(field),
                new FormatterOptions(template));
            var item = FilterTestHelpers.CreateRenameItem();

            filter.Setup();
            filter.Apply(item);

            const string nonNumericTheoryMessage = "Theory must only use numeric SemanticAudioField values.";
            var actual = field switch
            {
                SemanticAudioField.Year => item.Preview.AudioTagOverlay.Semantic().Year,
                SemanticAudioField.Track => item.Preview.AudioTagOverlay.Semantic().Track,
                SemanticAudioField.TrackCount => item.Preview.AudioTagOverlay.Semantic().TrackCount,
                SemanticAudioField.Disc => item.Preview.AudioTagOverlay.Semantic().Disc,
                SemanticAudioField.DiscCount => item.Preview.AudioTagOverlay.Semantic().DiscCount,
                SemanticAudioField.Title => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Album => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Performers => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.AlbumArtists => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Composers => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Genre => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Comment => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Lyrics => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Copyright => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.Grouping => throw new InvalidOperationException(nonNumericTheoryMessage),
                SemanticAudioField.BeatsPerMinute => throw new NotImplementedException(),
                SemanticAudioField.Conductor => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzArtistId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseArtistId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzTrackId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzDiscId => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseStatus => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseType => throw new NotImplementedException(),
                SemanticAudioField.MusicBrainzReleaseCountry => throw new NotImplementedException(),
                SemanticAudioField.MusicIpId => throw new NotImplementedException(),
                SemanticAudioField.AmazonId => throw new NotImplementedException(),
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
                new SemanticAudioFieldTarget(SemanticAudioField.Year),
                new FormatterOptions(string.Empty));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(year: 2001));

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Year);
        }

        /// <summary>
        /// Verifies whitespace-only template clears numeric overlay (same as empty).
        /// </summary>
        [Fact]
        public void Formatter_WhitespaceOnlyTemplate_ClearsNumericOverlayField()
        {
            var filter = new FormatterFilter(
                new SemanticAudioFieldTarget(SemanticAudioField.Track),
                new FormatterOptions("   "));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(track: 9));

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Track);
        }

        /// <summary>
        /// Verifies non-integer template text throws when assigning a numeric overlay field.
        /// </summary>
        [Fact]
        public void Formatter_InvalidNumericTemplate_ThrowsArgumentException()
        {
            var filter = new FormatterFilter(
                new SemanticAudioFieldTarget(SemanticAudioField.Disc),
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
                new SemanticAudioFieldTarget(SemanticAudioField.Year),
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
                    m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(year: 1999);
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(2009u, item.Preview.AudioTagOverlay.Semantic().Year);
        }

        /// <summary>
        /// Verifies replacer mutates the addressed overlay field.
        /// </summary>
        [Fact]
        public void Replacer_ReplacesGenreOnPreviewAudioOverlay()
        {
            var filter = new ReplacerFilter(
                new SemanticAudioFieldTarget(SemanticAudioField.Genre),
                new ReplacerOptions(
                    Find: "Rock",
                    Replacement: "Metal",
                    Mode: ReplacerMode.Literal,
                    CaseSensitive: true,
                    ReplaceAll: false,
                    WholeWord: false));

            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(genre: "Hard Rock"));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Hard Metal", item.Preview.AudioTagOverlay.Semantic().Genre);
        }

        /// <summary>
        /// Verifies directory rows cannot hydrate tags for overlay targets.
        /// </summary>
        [Fact]
        public void Apply_ToDirectory_ThrowsInvalidOperation()
        {
            var filter = new FormatterFilter(
                new SemanticAudioFieldTarget(SemanticAudioField.Title),
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
