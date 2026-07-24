using Mfr.Filters;
using Mfr.Metadata;
using Mfr.Models;
using Mfr.Tests.TestSupport;
using Mfr.Utils;

namespace Mfr.Tests.Models
{
    public sealed class RenameItemAudioTagsTests : IDisposable
    {
        private readonly string _tempRoot;

        public RenameItemAudioTagsTests()
        {
            _tempRoot = Directory.GetCurrentDirectory().CombinePath(
                "mfr_renameitem_audiotags_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }

        /// <summary>
        /// Verifies <see cref="RenameItemEmbeddedTagsExtensions.EnsureEmbeddedTagsLoaded"/> reads disk tags onto both snapshots.
        /// </summary>
        [Fact]
        public void EnsureEmbeddedTagsLoaded_ReadsFromDisk_MirrorsOntoBothSnapshots()
        {
            var path = Path.Combine(_tempRoot, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "DiskTitle", album: "SnapshotAlbum");

            var directory = Path.GetDirectoryName(path)!;
            var item = new RenameItem(new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directory,
                prefix: Path.GetFileNameWithoutExtension(path),
                extension: Path.GetExtension(path),
                renameListFolderSiblingCount: 1));

            var mutated = AudioTagSemanticSurface.FromBlocks(item.Preview.AudioTagOverlay)
                with
            { Album = "PreviewOnlyMutated" };
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(item.Preview.AudioTagOverlay, mutated, embeddedTagSourcePath: null);

            item.EnsureEmbeddedTagsLoaded();

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
