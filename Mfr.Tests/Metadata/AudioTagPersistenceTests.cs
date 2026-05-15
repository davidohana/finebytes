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
