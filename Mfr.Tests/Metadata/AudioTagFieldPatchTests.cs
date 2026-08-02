using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v1;
using Mfr.Utils;
using TagLib;
using TagLib.Id3v2;
using TagLib.Ogg;

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
            preview.MergeSemantic(merged);

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
        /// ASF title patch writes Content Description Title (TagLib façade) without Clear, and preserves other fields.
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
            preview.MergeSemantic(merged);

            AudioTagPersistence.Apply(path, original, preview);

            using (var file = TagLib.File.Create(path))
            {
                var asf = Assert.IsType<TagLib.Asf.Tag>(file.GetTag(TagTypes.Asf));
                Assert.Equal("AsfPatchedTitle", asf.Title);
                Assert.DoesNotContain(asf, d => string.Equals(d.Name, "WM/Title", StringComparison.Ordinal));
            }

            var after = AudioTagPersistence.Read(path);
            Assert.Equal("AsfPatchedTitle", after.Semantic().Title);
            Assert.NotNull(after.Asf);
            Assert.Contains(
                after.Asf.Descriptors,
                d => d.Name == AsfDescriptorNames.Title && d.Value == "AsfPatchedTitle");
            Assert.DoesNotContain(after.Asf.Descriptors, d => d.Name == "WM/Title");
            Assert.True(after.Asf.Descriptors.Length >= descriptorCountBefore - 1);
        }

        /// <summary>
        /// ASF performers, comment, and copyright map to Author / WM/Text / Copyright (TagLib façade).
        /// </summary>
        [Fact]
        public void Apply_Wma_CommonFields_WriteTagLibCanonicalLocations()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("asf-canonical.wma");
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "taglib-sharp-sample.wma");
            System.IO.File.Copy(fixturePath, path, overwrite: false);

            var original = AudioTagPersistence.Read(path);
            var preview = original.Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with
            {
                Title = "CanonTitle",
                Performers = "Canon Artist",
                Comment = "CanonComment",
                Copyright = "Canon©",
                Disc = 2,
                DiscCount = 3,
                TrackCount = 12,
            };
            preview.MergeSemantic(merged);
            AudioTagPersistence.Apply(path, original, preview);

            using var file = TagLib.File.Create(path);
            var asf = Assert.IsType<TagLib.Asf.Tag>(file.GetTag(TagTypes.Asf));
            Assert.Equal("CanonTitle", asf.Title);
            Assert.Equal(["Canon Artist"], asf.Performers);
            Assert.Equal("CanonComment", asf.Comment);
            Assert.Equal("Canon©", asf.Copyright);
            Assert.Equal(2u, asf.Disc);
            Assert.Equal(3u, asf.DiscCount);
            Assert.Equal(12u, asf.TrackCount);
            Assert.DoesNotContain(asf, d => string.Equals(d.Name, "WM/Title", StringComparison.Ordinal));
            Assert.DoesNotContain(asf, d => string.Equals(d.Name, "WM/Author", StringComparison.Ordinal));
            Assert.DoesNotContain(asf, d => string.Equals(d.Name, "WM/Description", StringComparison.Ordinal));
            Assert.DoesNotContain(asf, d => string.Equals(d.Name, "WM/ProviderCopyright", StringComparison.Ordinal));
            Assert.DoesNotContain(asf, d => string.Equals(d.Name, "WM/TotalDiscs", StringComparison.Ordinal));
            Assert.Contains(asf, d => string.Equals(d.Name, "WM/Text", StringComparison.Ordinal));
            Assert.Contains(
                asf,
                d => string.Equals(d.Name, "WM/PartOfSet", StringComparison.Ordinal)
                    && string.Equals(d.ToString(), "2/3", StringComparison.Ordinal));
            Assert.Contains(asf, d => string.Equals(d.Name, "TrackTotal", StringComparison.Ordinal));
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
        /// Clearing the last modeled ID3v2 frame prunes the block to null and removes APIC with the tag type.
        /// </summary>
        [Fact]
        public void Apply_Mp3_EmptyModeledId3v2Prune_DropsApic()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("prune-art.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v2Title: "OnlyModeled");
            _EmbedTinyPngCover(path, description: "cover");

            var original = AudioTagPersistence.Read(path);
            Assert.NotNull(original.Id3v2);
            Assert.NotEmpty(original.Id3v2.Frames);

            var preview = original.Clone();
            AudioOverlayBlockFieldIo.SetId3v2FrameString(preview, "TIT2", string.Empty);
            Assert.Null(preview.Id3v2);

            AudioTagPersistence.Apply(path, original, preview);

            using var after = TagLib.File.Create(path);
            Assert.Empty(after.Tag.Pictures);
            Assert.False(after.TagTypesOnDisk.HasFlag(TagTypes.Id3v2));
            Assert.Null(AudioTagPersistence.Read(path).Id3v2);
        }

        /// <summary>
        /// Clearing one ID3v1 scalar writes an empty field; clearing every scalar removes the trailer.
        /// </summary>
        [Fact]
        public void Apply_Mp3_Id3v1_SingleFieldClear_Vs_ClearAll()
        {
            var singlePath = _tempDirectoryFixture.CreateTempDir().CombinePath("id3v1-single.mp3");
            TaggedMp3Fixture.WriteTagged(singlePath, id3v1Title: "KeepArtistTitle", id3v2Title: "FrameStay");
            _SeedId3v1Artist(singlePath, "KeepArtist");

            var singleOriginal = AudioTagPersistence.Read(singlePath);
            Assert.Equal("KeepArtistTitle", singleOriginal.Id3v1!.Title);
            Assert.Equal("KeepArtist", singleOriginal.Id3v1.Artist);

            var singlePreview = singleOriginal.Clone();
            AudioOverlayBlockFieldIo.SetId3v1FieldString(singlePreview, Id3v1Field.Title, string.Empty);
            Assert.NotNull(singlePreview.Id3v1);
            Assert.Null(singlePreview.Id3v1.Title);
            Assert.Equal("KeepArtist", singlePreview.Id3v1.Artist);

            AudioTagPersistence.Apply(singlePath, singleOriginal, singlePreview);

            using (var afterSingle = TagLib.File.Create(singlePath))
            {
                var id3v1 = Assert.IsType<TagLib.Id3v1.Tag>(afterSingle.GetTag(TagTypes.Id3v1, false));
                Assert.True(string.IsNullOrEmpty(id3v1.Title));
                Assert.Equal("KeepArtist", id3v1.FirstPerformer);
                Assert.True(afterSingle.TagTypesOnDisk.HasFlag(TagTypes.Id3v1));
            }

            var clearAllPath = _tempDirectoryFixture.CreateTempDir().CombinePath("id3v1-clear-all.mp3");
            TaggedMp3Fixture.WriteTagged(clearAllPath, id3v1Title: "Gone", id3v2Title: "FrameStay");
            _SeedId3v1Artist(clearAllPath, "AlsoGone");

            var clearAllOriginal = AudioTagPersistence.Read(clearAllPath);
            var clearAllPreview = clearAllOriginal.Clone();
            AudioOverlayBlockFieldIo.SetId3v1FieldString(clearAllPreview, Id3v1Field.Title, string.Empty);
            AudioOverlayBlockFieldIo.SetId3v1FieldString(clearAllPreview, Id3v1Field.Artist, string.Empty);
            Assert.Null(clearAllPreview.Id3v1);

            AudioTagPersistence.Apply(clearAllPath, clearAllOriginal, clearAllPreview);

            using var afterClearAll = TagLib.File.Create(clearAllPath);
            Assert.False(afterClearAll.TagTypesOnDisk.HasFlag(TagTypes.Id3v1));
            Assert.Null(AudioTagPersistence.Read(clearAllPath).Id3v1);
            Assert.Equal("FrameStay", AudioTagPersistence.Read(clearAllPath).Id3v2!.Frames
                .Single(f => f.FrameId == "TIT2").TextValues[0]);
        }

        /// <summary>
        /// Clearing one COMM/TXXX instance leaves other instances with different identity intact.
        /// </summary>
        [Fact]
        public void Apply_Mp3_ClearPrimaryCommAndOneTxxx_PreservesOtherInstances()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("multi-instance.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v2Title: "TitleStay");
            _SeedMultiInstanceFrames(path);

            var original = AudioTagPersistence.Read(path);
            Assert.Contains(
                original.Id3v2!.Frames,
                f => f.FrameId == "COMM"
                    && string.Equals(f.Language, "eng", StringComparison.Ordinal)
                    && string.IsNullOrEmpty(f.Description));
            Assert.Contains(
                original.Id3v2.Frames,
                f => f.FrameId == "COMM"
                    && string.Equals(f.Language, "deu", StringComparison.Ordinal)
                    && string.Equals(f.Description, "liner", StringComparison.Ordinal));
            Assert.Contains(original.Id3v2.Frames, f => f.FrameId == "TXXX" && f.Description == "replaygain");
            Assert.Contains(original.Id3v2.Frames, f => f.FrameId == "TXXX" && f.Description == "catalog");

            var preview = original.Clone();
            AudioOverlayBlockFieldIo.SetId3v2FrameString(preview, "COMM", string.Empty);
            AudioOverlayBlockFieldIo.SetId3v2FrameString(
                preview,
                "TXXX",
                string.Empty,
                description: "replaygain");

            AudioTagPersistence.Apply(path, original, preview);

            using var after = TagLib.File.Create(path);
            var id3v2 = Assert.IsType<TagLib.Id3v2.Tag>(after.GetTag(TagTypes.Id3v2, false));

            var comments = id3v2.GetFrames<CommentsFrame>("COMM").ToArray();
            Assert.DoesNotContain(
                comments,
                c => string.Equals(c.Language, "eng", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrEmpty(c.Description));
            var secondaryComm = Assert.Single(
                comments,
                c => string.Equals(c.Language, "deu", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.Description, "liner", StringComparison.Ordinal));
            Assert.Equal("German liner", secondaryComm.Text);

            var userText = id3v2.GetFrames<UserTextInformationFrame>("TXXX").ToArray();
            Assert.DoesNotContain(userText, t => string.Equals(t.Description, "replaygain", StringComparison.Ordinal));
            var catalog = Assert.Single(userText, t => string.Equals(t.Description, "catalog", StringComparison.Ordinal));
            Assert.Equal("ABC-123", Assert.Single(catalog.Text));
            Assert.Equal("TitleStay", id3v2.Title);
        }

        /// <summary>
        /// Title-only Xiph patch leaves an unknown comment key on disk (known-key field patch only).
        /// </summary>
        [Fact]
        public void Apply_Flac_TitleOnlyPatch_PreservesUnknownXiphKey()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("unknown-xiph.flac");
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "metaflac.flac");
            System.IO.File.Copy(fixturePath, path, overwrite: false);

            using (var seed = TagLib.File.Create(path))
            {
                var xiph = (XiphComment)seed.GetTag(TagTypes.Xiph, true);
                xiph.SetField("TITLE", "BeforeTitle");
                xiph.SetField("MY_CUSTOM_KEY", "KeepMe");
                seed.Save();
            }

            var original = AudioTagPersistence.Read(path);
            Assert.DoesNotContain(original.Xiph!.Fields, r => r.Key == "MY_CUSTOM_KEY");

            var preview = original.Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with { Title = "AfterTitle" };
            preview.MergeSemantic(merged);
            AudioTagPersistence.Apply(path, original, preview);

            using var after = TagLib.File.Create(path);
            var afterXiph = (XiphComment)after.GetTag(TagTypes.Xiph, false);
            Assert.Equal(["AfterTitle"], afterXiph.GetField("TITLE"));
            Assert.Equal(["KeepMe"], afterXiph.GetField("MY_CUSTOM_KEY"));
        }

        /// <summary>
        /// Field-patch on an existing ID3v2.4 tag preserves version 4 (no silent downgrade/upgrade).
        /// </summary>
        [Fact]
        public void Apply_Mp3_Tit2Patch_PreservesId3v24Version()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("v24-preserve.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v2Title: "V24Title");
            using (var seed = TagLib.File.Create(path))
            {
                var id3v2 = (TagLib.Id3v2.Tag)seed.GetTag(TagTypes.Id3v2, true);
                id3v2.Version = 4;
                seed.Save();
            }

            var original = AudioTagPersistence.Read(path);
            Assert.Equal(4, original.Id3v2!.Version);

            var preview = original.Clone();
            AudioOverlayBlockFieldIo.SetId3v2FrameString(preview, "TIT2", "StillV24");
            Assert.Equal(4, preview.Id3v2!.Version);

            AudioTagPersistence.Apply(path, original, preview);

            using var after = TagLib.File.Create(path);
            var afterId3v2 = (TagLib.Id3v2.Tag)after.GetTag(TagTypes.Id3v2, false);
            Assert.Equal(4, afterId3v2.Version);
            Assert.Equal("StillV24", afterId3v2.Title);
            Assert.Equal(4, AudioTagPersistence.Read(path).Id3v2!.Version);
        }

        /// <summary>
        /// Expanded singleton frames such as <c>TPUB</c> are read into the overlay and field-patched.
        /// </summary>
        [Fact]
        public void Apply_Mp3_TpubPatch_RoundTrips()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("tpub-patch.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v2Title: "Song");
            using (var seed = TagLib.File.Create(path))
            {
                var id3v2 = (TagLib.Id3v2.Tag)seed.GetTag(TagTypes.Id3v2, true);
                id3v2.AddFrame(new TextInformationFrame("TPUB", StringType.UTF8) { Text = ["EMI"] });
                seed.Save();
            }

            var original = AudioTagPersistence.Read(path);
            Assert.Equal("EMI", AudioOverlayBlockFieldIo.GetId3v2FrameString(original, "TPUB"));

            var preview = original.Clone();
            AudioOverlayBlockFieldIo.SetId3v2FrameString(preview, "TPUB", "Parlophone");
            AudioTagPersistence.Apply(path, original, preview);

            var again = AudioTagPersistence.Read(path);
            Assert.Equal("Parlophone", AudioOverlayBlockFieldIo.GetId3v2FrameString(again, "TPUB"));
            Assert.Equal("Song", AudioOverlayBlockFieldIo.GetId3v2FrameString(again, "TIT2"));
        }

        /// <summary>
        /// v2.4-only frames such as <c>TMOO</c> are modeled when present on a version-4 tag.
        /// </summary>
        [Fact]
        public void Read_Mp3_TmooOnV24_IsModeled()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("tmoo-v24.mp3");
            TaggedMp3Fixture.WriteTagged(path, id3v2Title: "MoodSong");
            using (var seed = TagLib.File.Create(path))
            {
                var id3v2 = (TagLib.Id3v2.Tag)seed.GetTag(TagTypes.Id3v2, true);
                id3v2.Version = 4;
                id3v2.AddFrame(new TextInformationFrame("TMOO", StringType.UTF8) { Text = ["Melancholic"] });
                seed.Save();
            }

            var overlay = AudioTagPersistence.Read(path);
            Assert.Equal(4, overlay.Id3v2!.Version);
            Assert.Equal("Melancholic", AudioOverlayBlockFieldIo.GetId3v2FrameString(overlay, "TMOO"));
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

        private static void _SeedId3v1Artist(string path, string artist)
        {
            using var file = TagLib.File.Create(path);
            var id3v1 = (TagLib.Id3v1.Tag)file.GetTag(TagTypes.Id3v1, true);
            id3v1.Performers = [artist];
            file.Save();
        }

        private static void _SeedMultiInstanceFrames(string path)
        {
            using var file = TagLib.File.Create(path);
            var id3v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true);
            id3v2.RemoveFrames("COMM");
            id3v2.RemoveFrames("TXXX");
            id3v2.AddFrame(new CommentsFrame(string.Empty, "eng") { Text = "Primary eng" });
            id3v2.AddFrame(new CommentsFrame("liner", "deu") { Text = "German liner" });
            id3v2.AddFrame(new UserTextInformationFrame("replaygain") { Text = ["-6.0"] });
            id3v2.AddFrame(new UserTextInformationFrame("catalog") { Text = ["ABC-123"] });
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
