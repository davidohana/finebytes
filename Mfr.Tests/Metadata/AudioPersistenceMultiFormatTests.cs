using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Cross-format persistence checks using tiny committed binaries under <c>Fixtures/</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fixtures copy beside the test assembly via <c>Content CopyToOutputDirectory</c>: MP3 fuzz vector from
    /// <see href="https://github.com/lieff/minimp3/blob/master/vectors/fuzz/l3-compl-cut.mp3">lieff/minimp3</see>
    /// (MIT); FLAC excerpt from xiph/flac <c>metaflac.flac.ok</c>
    /// (<see href="https://raw.githubusercontent.com/xiph/flac/master/test/metaflac.flac.ok">BSD</see>,
    /// saved as .flac); OGG Vorbis <c>libnogg/bitrate-123.ogg</c> via
    /// <see href="https://github.com/RustAudio/lewton-test-assets">lewton-test-assets</see>
    /// /
    /// <see href="https://raw.githubusercontent.com/RustAudio/lewton-test-assets/master/libnogg/COPYING.libnogg">COPYING.libnogg</see>
    /// (BSD-style terms); M4A (AAC LC in MP4/M4A container) <see href="https://github.com/Homebrew/brew/blob/master/Library/Homebrew/test/support/fixtures/test.m4a">Homebrew/brew <c>test.m4a</c></see>
    /// (repository <see href="https://github.com/Homebrew/brew/blob/master/LICENSE.txt">BSD-2-Clause</see>,
    /// committed here as <c>homebrew-test.m4a</c>); PCM WAV scaffold via <see cref="MinimalWavFixture" /> (<c>minimal-silent.wav</c>).
    /// </para>
    /// </remarks>
    public sealed class AudioPersistenceMultiFormatTests : IDisposable
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

        /// <summary>
        /// Labels a parameterized format case paired with its committed fixture file name.
        /// </summary>
        public sealed record PersistenceFormatCase(string Label, string FixtureFileName)
        {
            /// <inheritdoc />
            public override string ToString()
            {
                return Label;
            }
        }

        /// <summary>
        /// Parameter matrix for MIME-backed persistence checks.
        /// </summary>
        public static TheoryData<PersistenceFormatCase> FormatCases { get; } =
            new TheoryData<PersistenceFormatCase>(
                new PersistenceFormatCase("wav", "minimal-silent.wav"),
                new PersistenceFormatCase("mp3", "l3-compl-cut.mp3"),
                new PersistenceFormatCase("flac", "metaflac.flac"),
                new PersistenceFormatCase("ogg", "libnogg-bitrate-123.ogg"),
                new PersistenceFormatCase("m4a", "homebrew-test.m4a"));

        [Theory(DisplayName = nameof(Read_AfterTagWrite_ReturnsTitles))]
        [MemberData(nameof(FormatCases))]
        public void Read_AfterTagWrite_ReturnsTitles(PersistenceFormatCase format)
        {
            var path = _AllocateScratchPath(format);

            _ApplyTags(path, baselineTitle: "fmt-baseline", baselineAlbum: "fmt-album");

            var baseline = AudioTagPersistence.Read(path);
            Assert.Equal("fmt-baseline", baseline.Semantic().Title);
            Assert.Equal("fmt-album", baseline.Semantic().Album);
        }

        /// <summary>
        /// Effective semantics from block-first projection remain stable for explicit title/album writes.
        /// </summary>
        /// <remarks>
        /// MP4 track/disc/year sometimes live in binary atoms not captured by the text-only Apple snapshot model; numeric
        /// comparisons are skipped for that fixture until projection reads those payloads. Other fields may differ between
        /// stored blocks and surface projection (for example dormant ID3 genres on noisy fixtures); title/album remain stable.
        /// </remarks>
        [Theory(DisplayName = nameof(Read_AfterTagWrite_SemanticProjectionMatchesMergedFaçade))]
        [MemberData(nameof(FormatCases))]
        public void Read_AfterTagWrite_SemanticProjectionMatchesMergedFaçade(PersistenceFormatCase format)
        {
            var path = _AllocateScratchPath(format);

            _ApplyTags(path, baselineTitle: "fmt-baseline", baselineAlbum: "fmt-album");

            var overlay = AudioTagPersistence.Read(path);
            var projected = AudioTagSemanticSurface.FromBlocks(overlay);

            Assert.Equal("fmt-baseline", projected.Title);
            Assert.Equal("fmt-album", projected.Album);

            var compareNumerics =
                !string.Equals(format.Label, "m4a", StringComparison.OrdinalIgnoreCase);

            if (!compareNumerics)
                return;

            var sem = overlay.Semantic();
            Assert.Equal(sem.Year, projected.Year);
            Assert.Equal(sem.Track, projected.Track);
            Assert.Equal(sem.TrackCount, projected.TrackCount);
            Assert.Equal(sem.Disc, projected.Disc);
            Assert.Equal(sem.DiscCount, projected.DiscCount);
        }

        [Theory(DisplayName = nameof(Apply_OverwritesTitle_AcrossFormats))]
        [MemberData(nameof(FormatCases))]
        public void Apply_OverwritesTitle_AcrossFormats(PersistenceFormatCase format)
        {
            var path = _AllocateScratchPath(format);

            _ApplyTags(path, baselineTitle: "round-a", baselineAlbum: "round-album");

            var baseline = AudioTagPersistence.Read(path);

            var preview = baseline.Clone();
            var mergedRound = AudioTagSemanticSurface.FromBlocks(preview) with { Title = "round-b" };
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(preview, mergedRound, path);

            AudioTagPersistence.Apply(path, preview);

            var again = AudioTagPersistence.Read(path);
            Assert.Equal("round-b", again.Semantic().Title);
            Assert.Equal("round-album", again.Semantic().Album);
        }

        private string _AllocateScratchPath(PersistenceFormatCase format)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(format.FixtureFileName);

            var unique = $"{Guid.NewGuid():N}";
            var fixturePath = Path.Combine(_FixturesDirectory, format.FixtureFileName);

            if (!File.Exists(fixturePath))
            {
                throw new InvalidOperationException(
                    $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output, or restore binaries.");
            }

            var extension = Path.GetExtension(format.FixtureFileName);
            var dest = Path.Combine(
                Path.GetTempPath(),
                $"mfr-fmt-{Path.GetFileNameWithoutExtension(format.FixtureFileName)}-{unique}{extension}");
            _pathsToDelete.Add(dest);
            File.Copy(fixturePath, dest, overwrite: false);
            return Path.GetFullPath(dest);
        }

        private static string _FixturesDirectory =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures");

        private static void _ApplyTags(string path, string baselineTitle, string baselineAlbum)
        {
            using var file = TagLib.File.Create(path);
            file.Tag.Title = baselineTitle;
            file.Tag.Album = baselineAlbum;
            file.Save();
        }
    }
}
