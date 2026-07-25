using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.Tests.Metadata
{
    public sealed class AudioTagPersistenceTests : IDisposable
    {
        private static readonly string[] s_AliceBobPerformers = ["Alice;Bob"];

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

            var preview = new AudioTagOverlay();

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
                var info = (TagLib.Riff.InfoTag)stub.GetTag(TagLib.TagTypes.RiffInfo, true);
                info.SetValue("INAM", "baseline");
                info.SetValue("IPRD", "AlbumX");
                stub.Save();
            }

            var readBaseline = AudioTagPersistence.Read(candidate);

            Assert.Equal("baseline", readBaseline.Semantic().Title);

            var previewOverlay = readBaseline.Clone();
            var merged = SemanticAudioTag.FromOverlay(previewOverlay) with { Title = "preview" };
            previewOverlay.MergeSemantic(merged);

            AudioTagPersistence.Apply(candidate, previewOverlay);

            var readAgain = AudioTagPersistence.Read(candidate);
            Assert.Equal("preview", readAgain.Semantic().Title);
            Assert.Equal("AlbumX", readAgain.Semantic().Album);
        }

        /// <summary>
        /// Verifies overlay performer strings land verbatim in the standard <c>IART</c> chunk and survive a read.
        /// </summary>
        [Fact]
        public void RoundTrip_Apply_PerformersWrittenToIartChunk()
        {
            var candidate = _AllocateMinimalWavPath();

            var readBaseline = AudioTagPersistence.Read(candidate);
            var previewOverlay = readBaseline.Clone();
            var merged = SemanticAudioTag.FromOverlay(previewOverlay)
                with
            { Performers = "Alice;Bob" };

            previewOverlay.MergeSemantic(merged);

            AudioTagPersistence.Apply(candidate, previewOverlay);

            var readAgain = AudioTagPersistence.Read(candidate);
            Assert.Equal("Alice;Bob", readAgain.Semantic().Performers);

            using var file = TagLib.File.Create(candidate);
            var info = (TagLib.Riff.InfoTag)file.GetTag(TagLib.TagTypes.RiffInfo, false);
            Assert.Equal(s_AliceBobPerformers, info.GetValuesAsStrings("IART"));
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
            Assert.Null(readBack.Semantic().Title);
            Assert.Null(readBack.Semantic().Album);
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

            using (var file = TagLib.File.Create(path))
            {
                var xiph = (TagLib.Ogg.XiphComment)file.GetTag(TagLib.TagTypes.Xiph, true);
                xiph.SetField("TITLE", "ogg-stable");
                file.Save();
            }

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Xiph);
            Assert.NotEmpty(first.Xiph.Fields);

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
                xiph.SetField("TITLE", "probe");
                file.Save();
            }

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Xiph);
            Assert.NotEmpty(first.Xiph.Fields);

            AudioTagPersistence.Apply(path, first.Clone());
            var second = AudioTagPersistence.Read(path);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Consecutive reads of the same M4A must yield equal overlays (deterministic Apple snapshot).
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
        /// Verifies <see cref="AudioTagOverlay.MergeSemantic"/> pushes a semantic title into the Xiph snapshot without saving.
        /// </summary>
        [Fact]
        public void MergeSemanticIntoBlocks_Ogg_MergesTitleIntoXiphSnapshot()
        {
            var path = _CopyFixtureToTemp("libnogg-bitrate-123.ogg");

            var disk = AudioTagPersistence.Read(path);
            var uniqueTitle = $"MergeOgg_{Guid.NewGuid():N}";
            var preview = disk.Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with { Title = uniqueTitle };
            preview.MergeSemantic(merged);

            Assert.NotNull(disk.Xiph);
            Assert.NotNull(preview.Xiph);
            Assert.NotEqual(disk.Xiph, preview.Xiph);
            Assert.Equal(uniqueTitle, preview.Semantic().Title);
        }

        /// <summary>
        /// Empty overlay + generic title creates the MPEG recommended ID3v2 block (v2.3), not ID3v1.
        /// </summary>
        [Fact]
        public void MergeSemanticIntoBlocks_EmptyOverlay_Mpeg_CreatesRecommendedId3v2()
        {
            var overlay = new AudioTagOverlay { ContainerFormat = AudioContainerFormat.Mpeg };
            var merged = new SemanticAudioTag(
                Title: "FromEmpty",
                Album: null,
                Performers: null,
                AlbumArtists: null,
                Composers: null,
                Genre: null,
                Comment: null,
                Lyrics: null,
                Copyright: null,
                Grouping: null,
                Year: null,
                Track: null,
                TrackCount: null,
                Disc: null,
                DiscCount: null);

            overlay.MergeSemantic(merged);

            Assert.NotNull(overlay.Id3v2);
            Assert.Null(overlay.Id3v1);
            Assert.Equal(3, overlay.Id3v2.Version);
            Assert.Equal("FromEmpty", overlay.Semantic().Title);
        }

        /// <summary>
        /// Empty overlay + generic title creates the FLAC recommended Xiph block only.
        /// </summary>
        [Fact]
        public void MergeSemanticIntoBlocks_EmptyOverlay_Flac_CreatesRecommendedXiph()
        {
            var overlay = new AudioTagOverlay { ContainerFormat = AudioContainerFormat.Flac };
            var merged = new SemanticAudioTag(
                Title: "FromEmptyFlac",
                Album: null,
                Performers: null,
                AlbumArtists: null,
                Composers: null,
                Genre: null,
                Comment: null,
                Lyrics: null,
                Copyright: null,
                Grouping: null,
                Year: null,
                Track: null,
                TrackCount: null,
                Disc: null,
                DiscCount: null);

            overlay.MergeSemantic(merged);

            Assert.NotNull(overlay.Xiph);
            Assert.Null(overlay.Ape);
            Assert.Equal("FromEmptyFlac", overlay.Semantic().Title);
        }

        /// <summary>
        /// Generic title broadcast updates every present block (ID3v1 and ID3v2) without inventing siblings.
        /// </summary>
        [Fact]
        public void MergeSemanticIntoBlocks_BroadcastsTitle_ToAllPresentBlocks()
        {
            var overlay = new AudioTagOverlay
            {
                ContainerFormat = AudioContainerFormat.Mpeg,
                Id3v1 = new Id3v1TagData { Title = "OldV1" },
                Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
            };
            var merged = new SemanticAudioTag(
                Title: "Broadcast",
                Album: null,
                Performers: null,
                AlbumArtists: null,
                Composers: null,
                Genre: null,
                Comment: null,
                Lyrics: null,
                Copyright: null,
                Grouping: null,
                Year: null,
                Track: null,
                TrackCount: null,
                Disc: null,
                DiscCount: null);

            overlay.MergeSemantic(merged);

            Assert.Equal("Broadcast", overlay.Id3v1.Title);
            Assert.Equal("Broadcast", overlay.Semantic().Title);
            Assert.Contains(overlay.Id3v2.Frames, f => f.FrameId == "TIT2" && f.TextValues[0] == "Broadcast");
            Assert.Null(overlay.Xiph);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagOverlay.MergeSemantic"/> merges a semantic title into the Apple snapshot for M4A.
        /// </summary>
        [Fact]
        public void MergeSemanticIntoBlocks_M4a_MergesTitleIntoAppleSnapshot()
        {
            var path = _CopyFixtureToTemp("homebrew-test.m4a");

            var disk = AudioTagPersistence.Read(path);
            Assert.NotNull(disk.Apple);

            var preview = disk.Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with { Title = "MergedM4aTitle" };

            preview.MergeSemantic(merged);

            Assert.NotEqual(disk.Apple, preview.Apple);
            Assert.Equal("MergedM4aTitle", preview.Semantic().Title);
        }

        /// <summary>
        /// A semantic-only title preview must merge into the Xiph block before <see cref="AudioTagPersistence.Apply"/> so Apply + Read stay consistent.
        /// </summary>
        [Fact]
        public void Apply_Flac_SemanticTitleChange_MergesXiphAndRoundTripsRead()
        {
            var path = _CopyFixtureToTemp("metaflac.flac");

            var disk = AudioTagPersistence.Read(path);
            var preview = disk.Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with { Title = "SemanticTitleMergeOnly" };
            preview.MergeSemantic(merged);

            AudioTagPersistence.Apply(path, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.Equal("SemanticTitleMergeOnly", after.Semantic().Title);
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
            Assert.NotEmpty(first.Ape.Fields);

            AudioTagPersistence.Apply(path, first.Clone());
            var second = AudioTagPersistence.Read(path);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Verifies APE alias keys and <c>number/total</c> pairs land on modeled keys and stay stable across Apply.
        /// </summary>
        [Fact]
        public void RoundTrip_Mp3_ApeAliasAndCountPair_NormalizeOntoModeledKeys()
        {
            var path = _AllocateMp3ScratchPath();

            using (var file = TagLib.File.Create(path))
            {
                var ape = (TagLib.Ape.Tag)file.GetTag(TagLib.TagTypes.Ape, true);
                ape.SetValue("ALBUMARTIST", "ape-album-artist");
                ape.SetValue("Track", "3/12");
                ape.SetValue("Disc", "1/2");
                file.Save();
            }

            var first = AudioTagPersistence.Read(path);
            Assert.NotNull(first.Ape);

            Assert.Equal("ape-album-artist", _ApeValue(first.Ape, "Album Artist"));
            Assert.Equal("3", _ApeValue(first.Ape, "Track"));
            Assert.Equal("12", _ApeValue(first.Ape, "TrackCount"));
            Assert.Equal("1", _ApeValue(first.Ape, "Disc"));
            Assert.Equal("2", _ApeValue(first.Ape, "DiscCount"));

            AudioTagPersistence.Apply(path, first.Clone());
            Assert.Equal(first, AudioTagPersistence.Read(path));
        }

        private static string? _ApeValue(ApeTagData ape, string key)
        {
            foreach (var row in ape.Fields)
            {
                if (!string.Equals(row.Key, key, StringComparison.Ordinal))
                    continue;

                return row.Values.Length == 0 ? null : row.Values[0];
            }

            return null;
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
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_mfr-minimal.wav");
            _pathsToDelete.Add(path);

            MinimalWavFixture.CopyScratchTo(path);
            return Path.GetFullPath(path);
        }
    }
}
