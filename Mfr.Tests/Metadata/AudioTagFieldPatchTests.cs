using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Utils;
using TagLib;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Golden tests for Original→Preview field-patch Apply (APIC survival, ASF no Clear, selective field clear).
    /// </summary>
    public sealed class AudioTagFieldPatchTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Title-only ID3v2 patch leaves an embedded APIC frame on disk.
        /// </summary>
        [Fact]
        public void Apply_Mp3_TitleOnlyPatch_PreservesApic()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("with-art.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v2Title: "Before");
            _EmbedTinyPngCover(path, description: "cover");

            var original = AudioTagPersistence.Read(path);
            Assert.NotNull(original.Id3v2);
            var preview = original.Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with { Title = "AfterTitleOnly" };
            AudioTagPersistence.MergeSemanticIntoBlocks(preview, merged);

            AudioTagPersistence.Apply(path, original, preview);

            using var after = TagLib.File.Create(path);
            Assert.Equal("AfterTitleOnly", after.Tag.Title);
            Assert.NotEmpty(after.Tag.Pictures);
            Assert.Equal("image/png", after.Tag.Pictures[0].MimeType);
        }

        /// <summary>
        /// Removing the ID3v2 block drops APIC with the tag type.
        /// </summary>
        [Fact]
        public void Apply_Mp3_RemoveId3v2Block_DropsApic()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("drop-art.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v2Title: "KeepText");
            _EmbedTinyPngCover(path, description: null);

            var original = AudioTagPersistence.Read(path);
            var preview = original.Clone();
            preview.ClearBlock(AudioTagBlockKind.Id3v2);

            AudioTagPersistence.Apply(path, original, preview);

            using var after = TagLib.File.Create(path);
            Assert.Empty(after.Tag.Pictures);
            Assert.Null(AudioTagPersistence.Read(path).Id3v2);
        }

        /// <summary>
        /// ASF title patch does not Clear the tag and round-trips.
        /// </summary>
        [Fact]
        public void Apply_Wma_TitleOnlyPatch_DoesNotClearAsf()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("asf-patch.wma");
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "taglib-sharp-sample.wma");
            System.IO.File.Copy(fixturePath, path, overwrite: false);

            var original = AudioTagPersistence.Read(path);
            Assert.NotNull(original.Asf);
            var descriptorCountBefore = original.Asf.Descriptors.Length;
            Assert.True(descriptorCountBefore > 1);

            var preview = original.Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with { Title = "AsfPatchedTitle" };
            AudioTagPersistence.MergeSemanticIntoBlocks(preview, merged);

            AudioTagPersistence.Apply(path, original, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.Equal("AsfPatchedTitle", after.Semantic().Title);
            Assert.NotNull(after.Asf);
            Assert.True(after.Asf.Descriptors.Length >= descriptorCountBefore - 1);
        }

        /// <summary>
        /// ID3v2 TIT2 patch leaves an unchanged ID3v1 trailer alone (no sibling rewrite).
        /// </summary>
        [Fact]
        public void Apply_Mp3_Tit2Only_LeavesId3v1Unchanged()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("tit2-only.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v1Title: "TrailerStay", id3v2Title: "OldFrame");

            var original = AudioTagPersistence.Read(path);
            var preview = original.Clone();
            AudioOverlayBlockFieldIo.SetId3v2FrameString(preview, "TIT2", "NewFrame");

            AudioTagPersistence.Apply(path, original, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.Equal("TrailerStay", after.Id3v1!.Title);
            Assert.Equal("NewFrame", AudioOverlayBlockFieldIo.GetId3v2FrameString(after, "TIT2"));
        }

        /// <summary>
        /// Embeds a 1x1 PNG cover via TagLib's <see cref="Picture"/> surface (avoids obsolete <c>AttachedPictureFrame</c>).
        /// </summary>
        private static void _EmbedTinyPngCover(string path, string? description)
        {
            using var file = TagLib.File.Create(path);
            file.Tag.Pictures =
            [
                new Picture
                {
                    MimeType = "image/png",
                    Type = PictureType.FrontCover,
                    Description = description ?? string.Empty,
                    Data = [.. _TinyPngBytes],
                },
            ];
            file.Save();
        }

        private static readonly byte[] _TinyPngBytes =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82,
        ];
    }
}
