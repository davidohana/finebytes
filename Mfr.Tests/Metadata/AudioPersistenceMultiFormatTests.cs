using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Cross-format persistence checks using tiny committed binaries plus synthetic WAV smoke.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fixtures (under <c>Fixtures/</c>, copied beside the test assembly): MP3 fuzz vector from
    /// <see href="https://github.com/lieff/minimp3/blob/master/vectors/fuzz/l3-compl-cut.mp3">lieff/minimp3</see>
    /// (MIT); FLAC excerpt from xiph/flac <c>metaflac.flac.ok</c>
    /// (<see href="https://raw.githubusercontent.com/xiph/flac/master/test/metaflac.flac.ok">BSD</see>,
    /// saved as .flac); OGG Vorbis <c>libnogg/bitrate-123.ogg</c> via
    /// <see href="https://github.com/RustAudio/lewton-test-assets">lewton-test-assets</see>
    /// / <see href="https://raw.githubusercontent.com/RustAudio/lewton-test-assets/master/libnogg/COPYING.libnogg">COPYING.libnogg</see>
    /// (BSD-style terms). WAV uses synthesized PCM skeleton bytes only (no external fixture file).
    /// </para>
    /// </remarks>
    public sealed class AudioPersistenceMultiFormatTests : IDisposable
    {
        private static readonly byte[] MinimalSilentWav =
        [
            0x52, 0x49, 0x46, 0x46, 0x28, 0x00, 0x00, 0x00,
            0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
            0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
            0x44, 0xAC, 0x00, 0x00, 0x88, 0x58, 0x01, 0x00,
            0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        ];

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
        /// Describes how an on-disk scenario is seeded for parameterized tests.
        /// </summary>
        public sealed record PersistenceFormatCase(string Label, PersistenceFormatSeed Seed, string? CommittedFixtureFileName)
        {
            /// <inheritdoc />
            public override string ToString()
            {
                return Label;
            }
        }

        /// <summary>
        /// Whether bytes come from synthesized WAV or copied committed fixtures.
        /// </summary>
        public enum PersistenceFormatSeed
        {
            /// <summary>PCM scaffold authored in UTF-8 source.</summary>
            SyntheticWav,

            /// <summary>Binary shipped under <c>Fixtures/</c> in this project.</summary>
            FixtureCopy,
        }

        /// <summary>
        /// Parameter matrix for MIME-backed persistence checks.
        /// </summary>
        public static TheoryData<PersistenceFormatCase> FormatCases { get; } =
            new TheoryData<PersistenceFormatCase>(
                new PersistenceFormatCase("wav", PersistenceFormatSeed.SyntheticWav, null),
                new PersistenceFormatCase("mp3", PersistenceFormatSeed.FixtureCopy, "l3-compl-cut.mp3"),
                new PersistenceFormatCase("flac", PersistenceFormatSeed.FixtureCopy, "metaflac.flac"),
                new PersistenceFormatCase("ogg", PersistenceFormatSeed.FixtureCopy, "libnogg-bitrate-123.ogg"));

        [Theory(DisplayName = nameof(Read_AfterTagWrite_ReturnsTitles))]
        [MemberData(nameof(FormatCases))]
        public void Read_AfterTagWrite_ReturnsTitles(PersistenceFormatCase format)
        {
            var path = _AllocateScratchPath(format);

            _ApplyTags(path, baselineTitle: "fmt-baseline", baselineAlbum: "fmt-album");

            var baseline = AudioTagPersistence.Read(path);
            Assert.NotNull(baseline);
            Assert.Equal("fmt-baseline", baseline.Title);
            Assert.Equal("fmt-album", baseline.Album);
        }

        [Theory(DisplayName = nameof(ApplyIfChanged_OverwritesTitle_AcrossFormats))]
        [MemberData(nameof(FormatCases))]
        public void ApplyIfChanged_OverwritesTitle_AcrossFormats(PersistenceFormatCase format)
        {
            var path = _AllocateScratchPath(format);

            _ApplyTags(path, baselineTitle: "round-a", baselineAlbum: "round-album");

            var baseline = AudioTagPersistence.Read(path);
            Assert.NotNull(baseline);

            var preview = baseline.Clone();
            preview.Title = "round-b";

            AudioTagPersistence.ApplyIfChanged(path, preview, baseline);

            var again = AudioTagPersistence.Read(path);
            Assert.NotNull(again);
            Assert.Equal("round-b", again.Title);
            Assert.Equal("round-album", again.Album);
        }

        private string _AllocateScratchPath(PersistenceFormatCase format)
        {
            var unique = $"{Guid.NewGuid():N}";

            switch (format.Seed)
            {
                case PersistenceFormatSeed.SyntheticWav:
                    var wavPath = Path.Combine(Path.GetTempPath(), $"mfr-fmt-{unique}.wav");
                    _pathsToDelete.Add(wavPath);
                    File.WriteAllBytes(wavPath, MinimalSilentWav);
                    return Path.GetFullPath(wavPath);

                case PersistenceFormatSeed.FixtureCopy:
                    ArgumentException.ThrowIfNullOrWhiteSpace(format.CommittedFixtureFileName);

                    var fixturePath = Path.Combine(_FixturesDirectory, format.CommittedFixtureFileName);
                    if (!File.Exists(fixturePath))
                    {
                        throw new InvalidOperationException(
                            $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output, or restore binaries.");
                    }

                    var extension = Path.GetExtension(format.CommittedFixtureFileName);
                    var dest = Path.Combine(
                        Path.GetTempPath(),
                        $"mfr-fmt-{Path.GetFileNameWithoutExtension(format.CommittedFixtureFileName)}-{unique}{extension}");
                    _pathsToDelete.Add(dest);
                    File.Copy(fixturePath, dest, overwrite: false);
                    return Path.GetFullPath(dest);

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format.Seed, "Unexpected fixture seed.");
            }
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
