using System.Collections.Immutable;
using Mfr.Filters;
using Mfr.Metadata;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v2;
using MetadataContainerDetector = Mfr.Metadata.AudioTagContainerDetector;
using ModelsContainerPolicy = Mfr.Models.Tags.AudioTagContainerPolicy;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Container capability policy: which tag blocks each format holds, and the errors raised when it cannot.
    /// </summary>
    public sealed class AudioTagContainerPolicyTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Container-to-supported-block matrix from the audio tag design.
        /// </summary>
        public static TheoryData<AudioContainerFormat, AudioTagBlockKind[], AudioTagBlockKind?> PolicyMatrix { get; } =
            new()
            {
                {
                    AudioContainerFormat.Mpeg,
                    [AudioTagBlockKind.Id3v1, AudioTagBlockKind.Id3v2],
                    AudioTagBlockKind.Id3v2
                },
                { AudioContainerFormat.Flac, [AudioTagBlockKind.Xiph, AudioTagBlockKind.Ape], AudioTagBlockKind.Xiph },
                { AudioContainerFormat.Ogg, [AudioTagBlockKind.Xiph], AudioTagBlockKind.Xiph },
                { AudioContainerFormat.Mpeg4, [AudioTagBlockKind.Apple], AudioTagBlockKind.Apple },
                { AudioContainerFormat.Asf, [AudioTagBlockKind.Asf], AudioTagBlockKind.Asf },
                { AudioContainerFormat.Riff, [AudioTagBlockKind.RiffInfo], AudioTagBlockKind.RiffInfo },
                { AudioContainerFormat.Ape, [AudioTagBlockKind.Ape], AudioTagBlockKind.Ape },
                { AudioContainerFormat.Unknown, [], null },
            };

        [Theory(DisplayName = nameof(GetSupportedBlocks_MatchesPolicyMatrix))]
        [MemberData(nameof(PolicyMatrix))]
        public void GetSupportedBlocks_MatchesPolicyMatrix(
            AudioContainerFormat container,
            AudioTagBlockKind[] expectedBlocks,
            AudioTagBlockKind? expectedRecommended
        )
        {
            Assert.Equal(expectedBlocks, ModelsContainerPolicy.GetSupportedBlocks(container));
            Assert.Equal(expectedRecommended, ModelsContainerPolicy.GetRecommendedBlock(container));

            foreach (var block in Enum.GetValues<AudioTagBlockKind>())
            {
                Assert.Equal(expectedBlocks.Contains(block), ModelsContainerPolicy.Supports(container, block));
            }
        }

        /// <summary>
        /// MPEG lists ID3v1 first for stable ordering but must recommend ID3v2 when creating a tag.
        /// </summary>
        [Fact]
        public void GetRecommendedBlock_Mpeg_PrefersId3v2OverId3v1()
        {
            Assert.Equal(AudioTagBlockKind.Id3v2, ModelsContainerPolicy.GetRecommendedBlock(AudioContainerFormat.Mpeg));
        }

        [Fact]
        public void EnsureSupported_Id3v2OnFlac_ThrowsNotSupported()
        {
            var ex = Assert.Throws<NotSupportedException>(() =>
                ModelsContainerPolicy.EnsureSupported(AudioContainerFormat.Flac, AudioTagBlockKind.Id3v2)
            );

            Assert.Contains("ID3v2", ex.Message, StringComparison.Ordinal);
            Assert.Contains("FLAC", ex.Message, StringComparison.Ordinal);
            Assert.Contains("Xiph comment", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void EnsureSupported_UnknownContainer_ReportsNoSupportedBlocks()
        {
            var ex = Assert.Throws<NotSupportedException>(() =>
                ModelsContainerPolicy.EnsureSupported(AudioContainerFormat.Unknown, AudioTagBlockKind.Xiph)
            );

            Assert.Contains("no tag blocks are supported", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void EnsureSupported_XiphOnFlac_DoesNotThrow()
        {
            ModelsContainerPolicy.EnsureSupported(AudioContainerFormat.Flac, AudioTagBlockKind.Xiph);
        }

        /// <summary>
        /// Fixture file name paired with the container it must resolve to.
        /// </summary>
        public static TheoryData<string, AudioContainerFormat> ContainerFixtures { get; } =
            new()
            {
                { "l3-compl-cut.mp3", AudioContainerFormat.Mpeg },
                { "metaflac.flac", AudioContainerFormat.Flac },
                { "libnogg-bitrate-123.ogg", AudioContainerFormat.Ogg },
                { "homebrew-test.m4a", AudioContainerFormat.Mpeg4 },
                { "taglib-sharp-sample.wma", AudioContainerFormat.Asf },
            };

        [Theory(DisplayName = nameof(Detect_Fixture_ResolvesContainer))]
        [MemberData(nameof(ContainerFixtures))]
        public void Detect_Fixture_ResolvesContainer(string fixtureFileName, AudioContainerFormat expected)
        {
            var path = _CopyFixtureToTempDir(fixtureFileName);

            Assert.Equal(expected, MetadataContainerDetector.Detect(path));
        }

        [Fact]
        public void Detect_MinimalWav_ResolvesRiff()
        {
            var path = _AllocateMinimalWavPath();

            Assert.Equal(AudioContainerFormat.Riff, MetadataContainerDetector.Detect(path));
        }

        [Fact]
        public void Detect_RelativePath_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => MetadataContainerDetector.Detect("relative\\only.mp3"));
            Assert.Contains("fully qualified", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The read that loads the overlay must also stamp the container so filters never reopen the file.
        /// </summary>
        [Fact]
        public void Read_Flac_StampsContainerFromSameOpen()
        {
            var path = _CopyFixtureToTempDir("metaflac.flac");
            using (var file = TagLib.File.Create(path))
            {
                file.Tag.Title = "ContainerReadTitle";
                file.Save();
            }

            var overlay = AudioTagPersistence.Read(path);

            Assert.Equal(AudioContainerFormat.Flac, overlay.ContainerFormat);
            Assert.True(overlay.HasBlock(AudioTagBlockKind.Xiph));
            Assert.False(overlay.HasBlock(AudioTagBlockKind.Id3v2));
        }

        /// <summary>
        /// A preview that adds a block the container cannot hold must fail loudly instead of writing a stray tag.
        /// </summary>
        [Fact]
        public void Apply_Flac_PreviewIntroducingId3v2_ThrowsNotSupported()
        {
            var path = _CopyFixtureToTempDir("metaflac.flac");

            var preview = AudioTagPersistence.Read(path).Clone();
            preview.Id3v2 = new Id3v2TagData
            {
                Version = 3,
                Frames = [new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["SpecificId3v2Title"] }],
            };

            var ex = Assert.Throws<NotSupportedException>(() => AudioTagPersistence.Apply(path, preview));
            Assert.Contains("ID3v2", ex.Message, StringComparison.Ordinal);
            Assert.Contains("FLAC", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Generic semantic writes stay unaffected by the policy: FLAC titles still land in the Xiph block.
        /// </summary>
        [Fact]
        public void Apply_Flac_GenericTitle_StillWritesXiph()
        {
            var path = _CopyFixtureToTempDir("metaflac.flac");

            var preview = AudioTagPersistence.Read(path).Clone();
            var merged = SemanticAudioTag.FromOverlay(preview) with { Title = "PolicyAllowsGenericTitle" };
            preview.MergeSemantic(merged);

            AudioTagPersistence.Apply(path, preview);

            var after = AudioTagPersistence.Read(path);
            Assert.Equal(AudioContainerFormat.Flac, after.ContainerFormat);
            Assert.Equal("PolicyAllowsGenericTitle", after.Semantic().Title);
            Assert.NotNull(after.Xiph);
            Assert.Null(after.Id3v2);
        }

        /// <summary>
        /// Loading embedded tags caches the container on both overlays so later filters can run capability checks for free.
        /// </summary>
        [Fact]
        public void EnsureTagLibLoaded_CachesDetectedContainerOnOverlays()
        {
            var path = _CopyFixtureToTempDir("metaflac.flac");
            var item = _CreateRenameItemFor(path);

            item.EnsureTagLibLoaded();

            Assert.Equal(AudioContainerFormat.Flac, item.Original.AudioTagOverlay.ContainerFormat);
            Assert.Equal(AudioContainerFormat.Flac, item.Preview.AudioTagOverlay.ContainerFormat);

            item.ClearEmbeddedTagsCache();
            Assert.Equal(AudioContainerFormat.Unknown, item.Preview.AudioTagOverlay.ContainerFormat);
        }

        [Fact]
        public void EnsureAudioTagBlockSupported_Id3v2OnFlacRow_ThrowsNotSupported()
        {
            var path = _CopyFixtureToTempDir("metaflac.flac");
            var item = _CreateRenameItemFor(path);

            item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Xiph);

            var ex = Assert.Throws<NotSupportedException>(() =>
                item.EnsureAudioTagBlockSupported(AudioTagBlockKind.Id3v2)
            );
            Assert.Contains("ID3v2", ex.Message, StringComparison.Ordinal);
        }

        private static RenameItem _CreateRenameItemFor(string absolutePath)
        {
            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: Path.GetDirectoryName(absolutePath)!,
                prefix: Path.GetFileNameWithoutExtension(absolutePath),
                extension: Path.GetExtension(absolutePath)
            );

            return new RenameItem(meta);
        }

        private string _CopyFixtureToTempDir(string fixtureFileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureFileName);
            Assert.True(
                File.Exists(fixturePath),
                $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output."
            );

            var dir = _tempDirectoryFixture.CreateTempDir();
            var dest = Path.Combine(dir, fixtureFileName);
            File.Copy(fixturePath, dest, overwrite: false);
            return Path.GetFullPath(dest);
        }

        private string _AllocateMinimalWavPath()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var path = Path.Combine(dir, "minimal.wav");
            MinimalWavFixture.CopyScratchTo(path);
            return Path.GetFullPath(path);
        }
    }
}
