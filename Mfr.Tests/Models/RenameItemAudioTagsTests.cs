using Mfr.Models;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models
{
    public sealed class RenameItemAudioTagsTests
    {
        /// <summary>
        /// Verifies <see cref="FilterTestHelpers.AudioTagReaderSnapshot"/> drives hydration like a mocked disk read, copying current <see cref="FileMeta.AudioTags"/> onto both snapshots.
        /// </summary>
        [Fact]
        public void EnsureAudioTagsLoaded_AudioTagReaderSnapshot_MirrorsMetaTagsOntoSnapshots()
        {
            var meta = _CreateMetaWithAlbum(album: "SnapshotAlbum");
            var item = new RenameItem(meta, FilterTestHelpers.AudioTagReaderSnapshot(meta));

            item.Preview.AudioTags.Album = "PreviewOnlyMutated";

            item.EnsureAudioTagsLoaded();

            Assert.Equal("SnapshotAlbum", item.Original.AudioTags.Album);
            Assert.Equal("SnapshotAlbum", item.Preview.AudioTags.Album);
        }

        [Fact]
        public void HasPreviewChanges_AudioTagOverlayMismatch_IsTrueWhilePathMatches()
        {
            var original = _CreateMetaWithAlbum("Baseline");
            var item = new RenameItem(original.Clone());

            Assert.False(item.HasPreviewChanges());

            item.Preview.AudioTags.Title = "PreviewTitle";

            Assert.True(item.HasPreviewChanges());
            Assert.True(item.IsPreviewPathUnchanged());
        }

        [Fact]
        public void CloneFileMeta_AudioTagsCopiedIndependently()
        {
            var first = _CreateMetaWithAlbum("A");
            var second = first.Clone();
            second.AudioTags.Title = "B";

            Assert.Null(first.AudioTags.Title);
            Assert.Equal("B", second.AudioTags.Title);
        }

        private static FileMeta _CreateMetaWithAlbum(string album)
        {
            return new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: Path.GetTempPath(),
                prefix: "x",
                extension: ".mp3",
                renameListFolderSiblingCount: 1)
            {
                AudioTags = new AudioTagOverlay { Album = album },
            };
        }
    }
}
