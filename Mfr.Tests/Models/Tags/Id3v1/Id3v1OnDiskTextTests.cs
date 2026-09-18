using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.Tests.Models.Tags.Id3v1
{
    /// <summary>
    /// Tests for <see cref="Id3v1OnDiskText"/> and ID3v1 overlay truncation.
    /// </summary>
    public sealed class Id3v1OnDiskTextTests
    {
        /// <summary>
        /// Verifies Title/Artist/Album clip to 30 Latin-1 bytes.
        /// </summary>
        [Fact]
        public void Truncate_title_artist_album_to_30_latin1_bytes()
        {
            var longTitle = new string('A', 40);
            var clipped = Id3v1OnDiskText.Truncate(longTitle, Id3v1OnDiskText.TitleArtistAlbumMaxBytes);

            Assert.Equal(30, clipped.Length);
            Assert.Equal(new string('A', 30), clipped);
        }

        /// <summary>
        /// Verifies Comment clips to 28 Latin-1 bytes (TagLib ID3v1.1 write layout).
        /// </summary>
        [Fact]
        public void Truncate_comment_to_28_latin1_bytes()
        {
            var longComment = new string('B', 40);
            var clipped = Id3v1OnDiskText.Truncate(longComment, Id3v1OnDiskText.CommentMaxBytes);

            Assert.Equal(28, clipped.Length);
            Assert.Equal(new string('B', 28), clipped);
        }

        /// <summary>
        /// Verifies Set/Get ID3v1 Title stores and resolves the clipped on-disk form.
        /// </summary>
        [Fact]
        public void SetId3v1FieldString_title_stores_truncated_preview()
        {
            var overlay = new AudioTagOverlay { ContainerFormat = AudioContainerFormat.Mpeg };
            var longTitle = new string('C', 40);

            AudioOverlayBlockFieldIo.SetId3v1FieldString(overlay, Id3v1Field.Title, longTitle);

            Assert.Equal(new string('C', 30), overlay.Id3v1!.Title);
            Assert.Equal(new string('C', 30), AudioOverlayBlockFieldIo.GetId3v1FieldString(overlay, Id3v1Field.Title));
        }

        /// <summary>
        /// Verifies Get clips legacy overlay values that were stored before truncation on write.
        /// </summary>
        [Fact]
        public void GetId3v1FieldString_clips_oversized_overlay_title()
        {
            var overlay = new AudioTagOverlay
            {
                ContainerFormat = AudioContainerFormat.Mpeg,
                Id3v1 = new Id3v1TagData { Title = new string('D', 40) },
            };

            Assert.Equal(new string('D', 30), AudioOverlayBlockFieldIo.GetId3v1FieldString(overlay, Id3v1Field.Title));
        }

        /// <summary>
        /// Verifies semantic merge into a present ID3v1 block stores the on-disk clipped form.
        /// </summary>
        [Fact]
        public void SemanticMerge_clips_id3v1_title_artist_album_comment()
        {
            var overlay = new AudioTagOverlay
            {
                ContainerFormat = AudioContainerFormat.Mpeg,
                Id3v1 = new Id3v1TagData(),
            };

            AudioTagSemanticMerge.MergeIntoPresentBlocks(
                overlay,
                new SemanticAudioTag(
                    Title: new string('T', 40),
                    Album: new string('A', 40),
                    Performers: new string('P', 40),
                    AlbumArtists: null,
                    Composers: null,
                    Genre: null,
                    Comment: new string('C', 40),
                    Lyrics: null,
                    Copyright: null,
                    Grouping: null,
                    Year: null,
                    Track: null,
                    TrackCount: null,
                    Disc: null,
                    DiscCount: null,
                    BeatsPerMinute: null,
                    Conductor: null,
                    MusicBrainzArtistId: null,
                    MusicBrainzReleaseId: null,
                    MusicBrainzReleaseArtistId: null,
                    MusicBrainzTrackId: null,
                    MusicBrainzDiscId: null,
                    MusicBrainzReleaseStatus: null,
                    MusicBrainzReleaseType: null,
                    MusicBrainzReleaseCountry: null,
                    MusicIpId: null,
                    AmazonId: null
                )
            );

            Assert.Equal(new string('T', 30), overlay.Id3v1!.Title);
            Assert.Equal(new string('A', 30), overlay.Id3v1.Album);
            Assert.Equal(new string('P', 30), overlay.Id3v1.Artist);
            Assert.Equal(new string('C', 28), overlay.Id3v1.Comment);
        }
    }
}
