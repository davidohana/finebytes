using Mfr.Filters;
using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Tests.Models
{
    public sealed class RenameItemAudioTagsTests : IDisposable
    {
        private readonly string _tempRoot;

        public RenameItemAudioTagsTests()
        {
            _tempRoot = Directory
                .GetCurrentDirectory()
                .CombinePath("mfr_renameitem_audiotags_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        /// <summary>
        /// Verifies <see cref="RenameItemEmbeddedTagsExtensions.EnsureTagLibLoaded"/> reads disk tags onto both snapshots.
        /// </summary>
        [Fact]
        public void EnsureTagLibLoaded_ReadsFromDisk_MirrorsOntoBothSnapshots()
        {
            var path = Path.Combine(_tempRoot, "tagged.wav");
            TaggedMinimalWav.WriteTagged(path, title: "DiskTitle", album: "SnapshotAlbum");

            var directory = Path.GetDirectoryName(path)!;
            var item = new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: directory,
                    prefix: Path.GetFileNameWithoutExtension(path),
                    extension: Path.GetExtension(path),
                    renameListFolderSiblingCount: 1
                )
            );

            var mutated = SemanticAudioTag.FromOverlay(item.Preview.AudioTagOverlay) with
            {
                Album = "PreviewOnlyMutated",
            };
            item.Preview.AudioTagOverlay.MergeSemantic(mutated);

            item.EnsureTagLibLoaded();

            Assert.Equal("SnapshotAlbum", item.Original.AudioTagOverlay.Semantic().Album);
            Assert.Equal("SnapshotAlbum", item.Preview.AudioTagOverlay.Semantic().Album);
        }

        /// <summary>
        /// Verifies one TagLib load fills both tag overlays and media properties.
        /// </summary>
        [Fact]
        public void EnsureTagLibLoaded_FillsTagsAndMedia()
        {
            var path = Path.Combine(_tempRoot, "tags-and-media.wav");
            TaggedMinimalWav.WriteTagged(path, title: "DiskTitle", album: "SnapshotAlbum");
            var item = _CreateUnmarkedItem(path);

            item.EnsureTagLibLoaded();

            Assert.True(item.TagLibLoadAttempted);
            Assert.Equal("SnapshotAlbum", item.Original.AudioTagOverlay.Semantic().Album);
            Assert.Equal("SnapshotAlbum", item.Preview.AudioTagOverlay.Semantic().Album);
            Assert.Equal(AudioContainerFormat.Riff, item.Original.AudioTagOverlay.ContainerFormat);
            var media = item.Original.Media;
            Assert.NotNull(media);
            Assert.Equal(media, item.Preview.Media);
            Assert.True(media.AudioChannels > 0);
        }

        [Fact]
        public void HasPreviewChanges_AudioTagOverlayMismatch_IsTrueWhilePathMatches()
        {
            var original = _CreateMetaWithAlbum("Baseline");
            var item = new RenameItem(original.Clone());

            Assert.False(item.HasPreviewChanges());

            var mergedPreview = SemanticAudioTag.FromOverlay(item.Preview.AudioTagOverlay) with
            {
                Title = "PreviewTitle",
            };
            item.Preview.AudioTagOverlay.MergeSemantic(mergedPreview);

            Assert.True(item.HasPreviewChanges());
            Assert.True(item.IsPreviewPathUnchanged());
        }

        [Fact]
        public void CloneFileMeta_AudioTagsCopiedIndependently()
        {
            var first = _CreateMetaWithAlbum("A");
            var second = first.Clone();
            var merged = SemanticAudioTag.FromOverlay(second.AudioTagOverlay) with { Title = "B" };
            second.AudioTagOverlay.MergeSemantic(merged);

            Assert.Null(first.AudioTagOverlay.Semantic().Title);
            Assert.Equal("B", second.AudioTagOverlay.Semantic().Title);
        }

        private static RenameItem _CreateUnmarkedItem(string path)
        {
            var directory = Path.GetDirectoryName(path)!;
            return new RenameItem(
                new FileMeta(
                    renameListIndex: 0,
                    inFolderIndex: 0,
                    directoryPath: directory,
                    prefix: Path.GetFileNameWithoutExtension(path),
                    extension: Path.GetExtension(path),
                    renameListFolderSiblingCount: 1
                )
            );
        }

        private static FileMeta _CreateMetaWithAlbum(string album)
        {
            return new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: Path.GetTempPath(),
                prefix: "x",
                extension: ".mp3",
                renameListFolderSiblingCount: 1
            )
            {
                AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(album: album),
            };
        }
    }
}
