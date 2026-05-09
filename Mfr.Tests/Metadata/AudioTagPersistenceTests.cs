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

        public void Read_MissingFullyQualifiedPath_ReturnsNull()

        {

            var overlay = AudioTagPersistence.Read(

                Path.Combine(Environment.CurrentDirectory, "___no_such_absolute___", "x.mp3"));



            Assert.Null(overlay);

        }



        [Fact]

        public void Read_RelativePath_ReturnsNull()

        {

            var overlay = AudioTagPersistence.Read("relative\\only.mp3");



            Assert.Null(overlay);

        }



        [Fact]

        public void ApplyIfChanged_TargetDirectoryPath_Throws()

        {

            var tempDir = Directory.CreateTempSubdirectory(prefix: "mfr-meta-");

            _pathsToDelete.Add(tempDir.FullName);



            var preview = new AudioTagOverlay { Title = "x" };

            var baseline = new AudioTagOverlay();



            var ex = Assert.Throws<ArgumentException>(() =>

                AudioTagPersistence.ApplyIfChanged(tempDir.FullName, preview, baseline));

            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);

        }



        [Fact]

        public void RoundTrip_ApplyIfChanged_OverwritesBaselineTitle()

        {

            var candidate = _AllocateMinimalWavPath();



            using (var stub = TagLib.File.Create(candidate))

            {

                stub.Tag.Title = "baseline";

                stub.Tag.Album = "AlbumX";

                stub.Save();



            }



            var readBaseline = AudioTagPersistence.Read(candidate);

            Assert.NotNull(readBaseline);



            Assert.Equal("baseline", readBaseline.Title);



            var previewOverlay = readBaseline.Clone();

            previewOverlay.Title = "preview";



            AudioTagPersistence.ApplyIfChanged(candidate, previewOverlay, readBaseline);



            var readAgain = AudioTagPersistence.Read(candidate);

            Assert.NotNull(readAgain);

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

