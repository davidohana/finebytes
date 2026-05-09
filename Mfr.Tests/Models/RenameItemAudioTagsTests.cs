using Mfr.Models;

namespace Mfr.Tests.Models
{
    public sealed class RenameItemAudioTagsTests
    {
        [Fact]
        public void HasPreviewChanges_AudioTagOverlayMismatch_IsTrueWhilePathMatches()
        {
            var original = CreateMetaWithAlbum("Baseline");
            var item = new RenameItem(original.Clone());

            Assert.False(item.HasPreviewChanges());

            item.Preview.AudioTags.Title = "PreviewTitle";

            Assert.True(item.HasPreviewChanges());
            Assert.True(item.IsPreviewPathUnchanged());
        }

        [Fact]
        public void CloneFileMeta_AudioTagsCopiedIndependently()
        {
            var first = CreateMetaWithAlbum("A");
            var second = first.Clone();
            second.AudioTags.Title = "B";

            Assert.Null(first.AudioTags.Title);
            Assert.Equal("B", second.AudioTags.Title);
        }

        private static FileMeta CreateMetaWithAlbum(string album)
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
