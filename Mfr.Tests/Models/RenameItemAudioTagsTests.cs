using Mfr.Metadata;
using Mfr.Models;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Models
{
    public sealed class RenameItemAudioTagsTests
    {
        /// <summary>
        /// Verifies <see cref="FilterTestHelpers.AudioTagReaderSnapshot"/> drives hydration like a mocked disk read, copying current <see cref="FileMeta.AudioTagOverlay"/> onto both snapshots.
        /// </summary>
        [Fact]
        public void EnsureAudioTagsLoaded_AudioTagReaderSnapshot_MirrorsMetaTagsOntoSnapshots()
        {
            var meta = _CreateMetaWithAlbum(album: "SnapshotAlbum");
            var item = new RenameItem(meta, FilterTestHelpers.AudioTagReaderSnapshot(meta));

            var mutated = AudioTagSemanticSurface.FromBlocks(item.Preview.AudioTagOverlay)
                with
            { Album = "PreviewOnlyMutated" };
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(item.Preview.AudioTagOverlay, mutated, embeddedTagSourcePath: null);

            item.EnsureAudioTagsLoaded();

            Assert.Equal("SnapshotAlbum", item.Original.AudioTagOverlay.Semantic().Album);
            Assert.Equal("SnapshotAlbum", item.Preview.AudioTagOverlay.Semantic().Album);
        }

        [Fact]
        public void HasPreviewChanges_AudioTagOverlayMismatch_IsTrueWhilePathMatches()
        {
            var original = _CreateMetaWithAlbum("Baseline");
            var item = new RenameItem(original.Clone());

            Assert.False(item.HasPreviewChanges());

            var mergedPreview = AudioTagSemanticSurface.FromBlocks(item.Preview.AudioTagOverlay) with { Title = "PreviewTitle" };
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(item.Preview.AudioTagOverlay, mergedPreview, embeddedTagSourcePath: null);

            Assert.True(item.HasPreviewChanges());
            Assert.True(item.IsPreviewPathUnchanged());
        }

        [Fact]
        public void CloneFileMeta_AudioTagsCopiedIndependently()
        {
            var first = _CreateMetaWithAlbum("A");
            var second = first.Clone();
            var merged = AudioTagSemanticSurface.FromBlocks(second.AudioTagOverlay) with { Title = "B" };
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(second.AudioTagOverlay, merged, embeddedTagSourcePath: null);

            Assert.Null(first.AudioTagOverlay.Semantic().Title);
            Assert.Equal("B", second.AudioTagOverlay.Semantic().Title);
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
                AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(album: album),
            };
        }
    }
}
