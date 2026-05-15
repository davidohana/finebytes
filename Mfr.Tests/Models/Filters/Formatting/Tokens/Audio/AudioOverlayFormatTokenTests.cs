using System.Globalization;
using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Audio;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Tests for audio overlay formatter tokens (<c>audio-*</c>).
    /// </summary>
    public sealed class AudioOverlayFormatTokenTests
    {
        private static AudioTagOverlay FullTagSample()
        {
            return new AudioTagOverlay
            {
                Title = "T1",
                Album = "Alb",
                Performers = "P1; P2",
                AlbumArtists = "AA",
                Genre = "Rock",
                Comment = "Co",
                Composers = "Comp",
                Lyrics = "Ly",
                Copyright = "Cop",
                Grouping = "Grp",
                Year = 2024,
                Track = 7,
                TrackCount = 12,
                Disc = 2,
                DiscCount = 3,
            };
        }

        [Fact]
        public void Resolve_TitleToken_ReturnsPreviewValue()
        {
            var token = new AudioTitleToken();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.AudioTagOverlay.Title = "Held");

            Assert.Equal("Held", token.Compile(string.Empty)(item));
            Assert.Contains("audio-title", token.Names);
        }

        [Fact]
        public void Resolve_WithAnyArgument_Throws()
        {
            var token = new AudioTitleToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("audio-title", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Resolve_PrefersPreviewOverlayOverOriginal()
        {
            var token = new AudioTitleToken();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.AudioTagOverlay.Title = "Orig");
            item.Preview.AudioTagOverlay.Title = "Prev";

            Assert.Equal("Prev", token.Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_EmptyDefaultOverlay_StringAndNumericYieldEmpty()
        {
            var title = new AudioTitleToken();
            var year = new AudioYearToken();
            var item = FilterTestHelpers.CreateRenameItem();

            Assert.Equal(string.Empty, title.Compile(string.Empty)(item));
            Assert.Equal(string.Empty, year.Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NumericUnset_YieldsEmpty_NotZeroLiteral()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Equal(string.Empty, new AudioYearToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new AudioTrackToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new AudioTrackCountToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new AudioDiscToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new AudioDiscCountToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NumericSet_UsesInvariantFormatting()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                var t = m.AudioTagOverlay;
                t.Year = 2024;
                t.Track = 7;
                t.TrackCount = 12;
                t.Disc = 2;
                t.DiscCount = 3;
            });

            Assert.Equal("2024", new AudioYearToken().Compile(string.Empty)(item));
            Assert.Equal("7", new AudioTrackToken().Compile(string.Empty)(item));
            Assert.Equal("12", new AudioTrackCountToken().Compile(string.Empty)(item));
            Assert.Equal("2", new AudioDiscToken().Compile(string.Empty)(item));
            Assert.Equal("3", new AudioDiscCountToken().Compile(string.Empty)(item));

            var prior = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.Equal("2024", new AudioYearToken().Compile(string.Empty)(item));
            }
            finally
            {
                CultureInfo.CurrentCulture = prior;
            }
        }

        [Fact]
        public void Resolve_NumericZeroWhenSet_YieldsZeroString()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.AudioTagOverlay.Year = 0;
                m.AudioTagOverlay.Track = 0;
            });

            Assert.Equal("0", new AudioYearToken().Compile(string.Empty)(item));
            Assert.Equal("0", new AudioTrackToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_AllStringFields_MappedFromSample()
        {
            var sample = FullTagSample();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.AudioTagOverlay = sample.Clone());

            Assert.Equal("T1", new AudioTitleToken().Compile(string.Empty)(item));
            Assert.Equal("Alb", new AudioAlbumToken().Compile(string.Empty)(item));
            Assert.Equal("P1; P2", new AudioArtistToken().Compile(string.Empty)(item));
            Assert.Equal("AA", new AudioAlbumArtistToken().Compile(string.Empty)(item));
            Assert.Equal("Rock", new AudioGenreToken().Compile(string.Empty)(item));
            Assert.Equal("Co", new AudioCommentToken().Compile(string.Empty)(item));
            Assert.Equal("Comp", new AudioComposerToken().Compile(string.Empty)(item));
            Assert.Equal("Ly", new AudioLyricsToken().Compile(string.Empty)(item));
            Assert.Equal("Cop", new AudioCopyrightToken().Compile(string.Empty)(item));
            Assert.Equal("Grp", new AudioGroupingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Apply_FormatterCombinesYearAndTitle()
        {
            var target = new FilePrefixTarget();
            var filter = new FormatterFilter(target, new FormatterOptions("<audio-year>-<audio-title>"));
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                configureOriginal: m =>
                {
                    m.AudioTagOverlay.Year = 1999;
                    m.AudioTagOverlay.Title = "Blue";
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("1999-Blue", item.Preview.Prefix);
        }
    }
}
