using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Apply-side tag-type removal: a block the file carries but the preview dropped is deleted outright.
    /// </summary>
    public sealed class AudioTagBlockRemovalTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        public void Apply_Mp3_PreviewDropsId3v1_RemovesTrailerAndKeepsFrames()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("drop-id3v1.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v1Title: "TrailerTitle", id3v2Title: "FrameTitle");

            var preview = AudioTagPersistence.Read(path).Clone();
            Assert.NotNull(preview.Id3v1);
            preview.ClearBlock(AudioTagBlockKind.Id3v1);

            AudioTagPersistence.Apply(path, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.Null(after.Id3v1);
            Assert.NotNull(after.Id3v2);
            Assert.Equal("FrameTitle", after.Semantic().Title);
        }

        [Fact]
        public void Apply_Flac_PreviewDropsXiph_RemovesComment()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("drop-xiph.flac");
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "metaflac.flac");
            File.Copy(fixturePath, path, overwrite: false);
            using (var file = TagLib.File.Create(path))
            {
                file.Tag.Title = "XiphTitle";
                file.Save();
            }

            var preview = AudioTagPersistence.Read(path).Clone();
            Assert.NotNull(preview.Xiph);
            preview.ClearBlock(AudioTagBlockKind.Xiph);

            AudioTagPersistence.Apply(path, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.Null(after.Xiph);
            Assert.Null(after.Semantic().Title);
        }

        [Fact]
        public void Apply_M4a_PreviewDropsApple_LeavesFileUntagged()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("drop-apple.m4a");
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "homebrew-test.m4a");
            File.Copy(fixturePath, path, overwrite: false);
            using (var file = TagLib.File.Create(path))
            {
                file.Tag.Title = "AppleTitle";
                file.Save();
            }

            var preview = AudioTagPersistence.Read(path).Clone();
            Assert.NotNull(preview.Apple);
            preview.ClearBlock(AudioTagBlockKind.Apple);

            AudioTagPersistence.Apply(path, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.False(after.HasAnyBlock());
            Assert.Null(after.Semantic().Title);
        }
    }
}
