using System.Globalization;
using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Audio;
using Mfr.Metadata;
using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Tests for audio overlay formatter tokens (<c>audio-*</c>).
    /// </summary>
    public sealed class AudioOverlayFormatTokenTests
    {
        private static AudioTagOverlay _FullTagSample()
        {
            return AudioTagOverlayTestBuilder.Id3Overlay(
                title: "T1",
                album: "Alb",
                performersJoined: "P1; P2",
                albumArtistsJoined: "AA",
                genre: "Rock",
                comment: "Co",
                composersJoined: "Comp",
                lyrics: "Ly",
                copyright: "Cop",
                grouping: "Grp",
                year: 2024,
                track: 7,
                trackCount: 12,
                disc: 2,
                discCount: 3);
        }

        [Fact]
        public void Resolve_TitleToken_ReturnsPreviewValue()
        {
            var token = new AudioTitleToken();
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Held"));

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
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Orig"));

            var merged = AudioTagSemanticSurface.FromBlocks(item.Preview.AudioTagOverlay) with { Title = "Prev" };
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(item.Preview.AudioTagOverlay, merged, embeddedTagSourcePath: null);

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
        public void Resolve_NumericSet_UsesDecimalDigitFormatting()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(
                    year: 2024,
                    track: 7,
                    trackCount: 12,
                    disc: 2,
                    discCount: 3);
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
        public void Resolve_NumericStoredAsZero_YieldsEmpty_NotZeroLiteral()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(year: 0, track: 0);
            });

            Assert.Equal(string.Empty, new AudioYearToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new AudioTrackToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_AllStringFields_MappedFromSample()
        {
            var sample = _FullTagSample();
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
                    m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Blue", year: 1999);
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("1999-Blue", item.Preview.Prefix);
        }
    }
}
