using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Metadata
{
    public sealed class AudioTagPersistenceTests : IDisposable
    {
        private static readonly string[] s_AliceBobPerformers = ["Alice", "Bob"];

        private readonly List<string> _pathsToDelete = [];

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var path in _pathsToDelete)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive: true);
                        continue;
                    }

                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (IOException)
                {
                }
            }
        }

        [Fact]
        public void Read_MissingFile_ThrowsArgumentException()
        {
            var path = Path.Combine(
                Environment.CurrentDirectory,
                "___no_such_absolute___",
                "x.mp3");

            Assert.False(File.Exists(path));
            Assert.True(Path.IsPathFullyQualified(path));

            var ex = Assert.Throws<ArgumentException>(() => AudioTagPersistence.Read(path));
            Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_RelativePath_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => AudioTagPersistence.Read("relative\\only.mp3"));
            Assert.Contains("fully qualified", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Apply_TargetDirectoryPath_Throws()
        {
            var tempDir = Directory.CreateTempSubdirectory(prefix: "mfr-meta-");
            _pathsToDelete.Add(tempDir.FullName);

            var preview = new AudioTagOverlay { Title = "x" };

            var ex = Assert.Throws<ArgumentException>(() =>
                AudioTagPersistence.Apply(tempDir.FullName, preview));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RoundTrip_Apply_OverwritesBaselineTitle()
        {
            var candidate = _AllocateMinimalWavPath();

            using (var stub = TagLib.File.Create(candidate))
            {
                stub.Tag.Title = "baseline";
                stub.Tag.Album = "AlbumX";
                stub.Save();
            }

            var readBaseline = AudioTagPersistence.Read(candidate);

            Assert.Equal("baseline", readBaseline.Title);

            var previewOverlay = readBaseline.Clone();
            previewOverlay.Title = "preview";

            AudioTagPersistence.Apply(candidate, previewOverlay);

            var readAgain = AudioTagPersistence.Read(candidate);
            Assert.Equal("preview", readAgain.Title);
            Assert.Equal("AlbumX", readAgain.Album);
        }

        /// <summary>
        /// Verifies overlay performer strings split on <c>;</c> for TagLib and rejoin as <c>; </c> on read.
        /// </summary>
        [Fact]
        public void RoundTrip_Apply_PerformersJoinedWithSemicolon()
        {
            var candidate = _AllocateMinimalWavPath();

            var readBaseline = AudioTagPersistence.Read(candidate);
            var previewOverlay = readBaseline.Clone();
            previewOverlay.Performers = "Alice;Bob";

            AudioTagPersistence.Apply(candidate, previewOverlay);

            var readAgain = AudioTagPersistence.Read(candidate);
            Assert.Equal("Alice; Bob", readAgain.Performers);

            using var file = TagLib.File.Create(candidate);
            Assert.Equal(s_AliceBobPerformers, file.Tag.Performers);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagPersistence.RemoveAllEmbeddedTags"/> clears all modeled fields read back from disk.
        /// </summary>
        [Fact]
        public void RemoveAllEmbeddedTags_ClearsAllTags_OnMinimalWav()
        {
            var candidate = _AllocateMinimalWavPath();

            using (var stub = TagLib.File.Create(candidate))
            {
                stub.Tag.Title = "t";
                stub.Tag.Album = "a";
                stub.Save();
            }

            AudioTagPersistence.RemoveAllEmbeddedTags(candidate);

            var readBack = AudioTagPersistence.Read(candidate);
            Assert.Null(readBack.Title);
            Assert.Null(readBack.Album);
        }

        /// <summary>
        /// Verifies MP3/MPEG reads expose <see cref="AudioTagOverlay.Id3v2"/> with at least one frame after TagLib writes.
        /// </summary>
        [Fact]
        public void Read_Mp3_WithWrittenTags_PopulatesId3v2Frames()
        {
            var candidate = _AllocateMp3ScratchPath();

            using (var file = TagLib.File.Create(candidate))
            {
                file.Tag.Title = "mpeg-title";
                file.Tag.Album = "mpeg-album";
                file.Save();
            }

            var overlay = AudioTagPersistence.Read(candidate);
            Assert.NotNull(overlay.Id3v2);
            Assert.NotEmpty(overlay.Id3v2.Frames);
            Assert.All(overlay.Id3v2.Frames, f => Assert.Equal(4, f.FrameId.Length));
        }

        /// <summary>
        /// Verifies identity Apply after full MP3 read leaves <see cref="AudioTagPersistence.Read"/> output equal.
        /// </summary>
        [Fact]
        public void RoundTrip_Mp3_Apply_ClonedRead_IsNoOpAndStable()
        {
            var candidate = _AllocateMp3ScratchPath();

            using (var file = TagLib.File.Create(candidate))
            {
                file.Tag.Title = "stable";
                file.Tag.Album = "alb";
                file.Save();
            }

            var first = AudioTagPersistence.Read(candidate);
            Assert.Equal(first, first.Clone());
            Assert.Equal(first, AudioTagPersistence.Read(candidate));

            AudioTagPersistence.Apply(candidate, first.Clone());
            var second = AudioTagPersistence.Read(candidate);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Identity Apply on fixture OGG should be a no-op and keep <see cref="AudioTagOverlay.Xiph"/> stable.
        /// </summary>
        [Fact]
        public void RoundTrip_Ogg_Apply_ClonedRead_IsNoOpAndStable()
        {
            var path = _CopyFixtureToTemp("libnogg-bitrate-123.ogg");

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Xiph);
            Assert.NotEmpty(first.Xiph.CanonicalTagBytes);

            AudioTagPersistence.Apply(path, first.Clone());
            var second = AudioTagPersistence.Read(path);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Identity Apply on fixture FLAC should be a no-op and keep <see cref="AudioTagOverlay.Xiph"/> stable.
        /// </summary>
        [Fact]
        public void RoundTrip_Flac_Apply_ClonedRead_IsNoOpAndStable()
        {
            var path = _CopyFixtureToTemp("metaflac.flac");

            using (var file = TagLib.File.Create(path))
            {
                var xiph = (TagLib.Ogg.XiphComment)file.GetTag(TagLib.TagTypes.Xiph, true);
                xiph.SetField("TRIPTEST", "probe");
                file.Save();
            }

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Xiph);
            Assert.NotEmpty(first.Xiph.CanonicalTagBytes);

            AudioTagPersistence.Apply(path, first.Clone());
            var second = AudioTagPersistence.Read(path);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Consecutive reads of the same M4A must yield equal overlays (deterministic Apple snapshot + façade).
        /// </summary>
        [Fact]
        public void Read_M4a_Twice_ReturnsEqualOverlays()
        {
            var path = _CopyFixtureToTemp("homebrew-test.m4a");
            var a = AudioTagPersistence.Read(path);
            var b = AudioTagPersistence.Read(path);
            Assert.Equal(a, b);
            Assert.Equal(a, a.Clone());
        }

        /// <summary>
        /// Identity Apply on fixture M4A should be a no-op and keep <see cref="AudioTagOverlay.Apple"/> stable.
        /// </summary>
        [Fact]
        public void RoundTrip_M4a_Apply_ClonedRead_IsNoOpAndStable()
        {
            var path = _CopyFixtureToTemp("homebrew-test.m4a");

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Apple);
            Assert.NotEmpty(first.Apple.Atoms);

            AudioTagPersistence.Apply(path, first.Clone());
            var second = AudioTagPersistence.Read(path);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagPersistence.MaterializePreviewFacadeIntoNativeBlocks"/> merges a façade title into the Xiph
        /// snapshot without saving (same rules as <see cref="AudioTagPersistence.Apply"/> coalesce).
        /// </summary>
        [Fact]
        public void MaterializePreview_Ogg_MergesTitleIntoXiphSnapshot()
        {
            var path = _CopyFixtureToTemp("libnogg-bitrate-123.ogg");

            var disk = AudioTagPersistence.Read(path);
            var uniqueTitle = $"MaterializeOgg_{Guid.NewGuid():N}";
            var preview = disk.Clone();
            preview.Title = uniqueTitle;

            AudioTagPersistence.MaterializePreviewFacadeIntoNativeBlocks(preview, path);

            Assert.NotNull(disk.Xiph);
            Assert.NotNull(preview.Xiph);
            Assert.NotEqual(disk.Xiph.CanonicalTagBytes, preview.Xiph.CanonicalTagBytes);
            Assert.Equal(uniqueTitle, preview.Title);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagPersistence.MaterializePreviewFacadeIntoNativeBlocks"/> merges façade fields into the Apple
        /// snapshot for M4A when given the on-disk source path.
        /// </summary>
        [Fact]
        public void MaterializePreview_M4a_MergesTitleIntoAppleSnapshot()
        {
            var path = _CopyFixtureToTemp("homebrew-test.m4a");

            var disk = AudioTagPersistence.Read(path);
            Assert.NotNull(disk.Apple);

            var preview = disk.Clone();
            preview.Title = "MaterializedM4aTitle";

            AudioTagPersistence.MaterializePreviewFacadeIntoNativeBlocks(preview, path);

            Assert.NotEqual(disk.Apple, preview.Apple);
            Assert.Equal("MaterializedM4aTitle", preview.Title);
        }

        /// <summary>
        /// Preview that only changes façade <see cref="AudioTagOverlay.Title"/> must coalesce into the Xiph block so Apply + Read stay consistent.
        /// </summary>
        [Fact]
        public void Apply_Flac_SemanticTitleChange_CoalescesXiphAndRoundTripsRead()
        {
            var path = _CopyFixtureToTemp("metaflac.flac");

            var disk = AudioTagPersistence.Read(path);
            var preview = disk.Clone();
            preview.Title = "SemanticTitleOnlyPhase3";

            AudioTagPersistence.Apply(path, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.Equal("SemanticTitleOnlyPhase3", after.Title);
            Assert.NotNull(after.Xiph);
            Assert.Equal(after, AudioTagPersistence.Read(path));
        }

        /// <summary>
        /// Identity Apply on fixture WMA should be a no-op and keep <see cref="AudioTagOverlay.Asf"/> stable.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Fixture <c>taglib-sharp-sample.wma</c> is the TagLib# test asset
        /// <see href="https://github.com/mono/taglib-sharp/blob/main/tests/TaglibSharp.Tests/samples/sample.wma">sample.wma</see>
        /// (same project as TagLibSharp on NuGet).
        /// </para>
        /// </remarks>
        [Fact]
        public void RoundTrip_Wma_Asf_Apply_ClonedRead_IsNoOpAndStable()
        {
            var path = _CopyFixtureToTemp("taglib-sharp-sample.wma");

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Asf);
            Assert.NotEmpty(first.Asf.Descriptors);

            AudioTagPersistence.Apply(path, first.Clone());
            var second = AudioTagPersistence.Read(path);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Verifies APE tags round-trip on a scratch MP3 when TagLib attaches an APE block.
        /// </summary>
        [Fact]
        public void RoundTrip_Mp3_WithApe_Apply_ClonedRead_IsNoOpAndStable()
        {
            var path = _AllocateMp3ScratchPath();

            using (var file = TagLib.File.Create(path))
            {
                var ape = (TagLib.Ape.Tag)file.GetTag(TagLib.TagTypes.Ape, true);
                ape.SetValue("Title", "ape-title");
                file.Save();
            }

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Ape);
            Assert.NotEmpty(first.Ape.CanonicalTagBytes);

            AudioTagPersistence.Apply(path, first.Clone());
            var second = AudioTagPersistence.Read(path);

            Assert.Equal(first, second);
        }

        private string _CopyFixtureToTemp(string fileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            if (!File.Exists(fixturePath))
            {
                throw new InvalidOperationException(
                    $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output.");
            }

            var dest = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_{fileName}");
            _pathsToDelete.Add(dest);
            File.Copy(fixturePath, dest, overwrite: false);
            return Path.GetFullPath(dest);
        }

        private string _AllocateMp3ScratchPath()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "l3-compl-cut.mp3");
            if (!File.Exists(fixturePath))
            {
                throw new InvalidOperationException(
                    $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output.");
            }

            var dest = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_mfr-mp3.mp3");
            _pathsToDelete.Add(dest);
            File.Copy(fixturePath, dest, overwrite: false);
            return Path.GetFullPath(dest);
        }

        private string _AllocateMinimalWavPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_mfr-phase1.wav");
            _pathsToDelete.Add(path);

            MinimalWavFixture.CopyScratchTo(path);
            return Path.GetFullPath(path);
        }
    }
}
