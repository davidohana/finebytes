using Mfr.Metadata;
using Mfr.Tests.TestSupport;

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
