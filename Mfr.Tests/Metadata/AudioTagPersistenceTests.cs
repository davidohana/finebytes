using Mfr.Metadata;
using Mfr.Models;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Metadata
{
    public sealed class AudioTagPersistenceTests : IDisposable
    {
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

        private string _AllocateMinimalWavPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_mfr-phase1.wav");
            _pathsToDelete.Add(path);

            MinimalWavFixture.CopyScratchTo(path);
            return Path.GetFullPath(path);
        }
    }
}
