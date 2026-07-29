using Mfr.Metadata;

namespace Mfr.Tests.Metadata
{
    /// <summary>
    /// Tests for <see cref="MediaPropertiesReader"/>.
    /// </summary>
    public sealed class MediaPropertiesReaderTests
    {
        [Fact]
        public void Read_Mp3Fixture_PopulatesAudioFields()
        {
            var path = _RequireFixture("l3-compl-cut.mp3");

            var media = MediaPropertiesReader.Read(path);

            Assert.False(string.IsNullOrWhiteSpace(media.MimeType));
            Assert.Contains("Audio", media.MediaTypes ?? string.Empty, StringComparison.Ordinal);
            Assert.True(media.AudioChannels > 0);
            Assert.True(media.AudioSampleRate > 0);
            Assert.True(media.Duration > TimeSpan.Zero || media.AudioBitrate > 0);
            Assert.Equal(0, media.VideoWidth);
            Assert.Equal(0, media.PhotoWidth);
        }

        [Fact]
        public void ReadStream_Mp3Fixture_PopulatesMediaAndMpeg()
        {
            var path = _RequireFixture("l3-compl-cut.mp3");

            var snapshot = MediaPropertiesReader.ReadStream(path);

            Assert.NotNull(snapshot.Mpeg);
            Assert.Equal(3, snapshot.Mpeg.Layer);
            Assert.False(snapshot.Mpeg.IsVbr);
            Assert.Equal("1", snapshot.Mpeg.MpegVersion);
            Assert.True(snapshot.Mpeg.Bitrate > 0);
            Assert.True(snapshot.Mpeg.SampleRate > 0);
            Assert.False(string.IsNullOrEmpty(snapshot.Mpeg.ChannelMode));
            Assert.Contains("Audio", snapshot.Media.MediaTypes ?? string.Empty, StringComparison.Ordinal);
        }

        [Fact]
        public void Read_WavFixture_PopulatesAudioFields()
        {
            var path = _RequireFixture("minimal-silent.wav");

            var media = MediaPropertiesReader.Read(path);

            Assert.False(string.IsNullOrWhiteSpace(media.MimeType));
            Assert.Contains("Audio", media.MediaTypes ?? string.Empty, StringComparison.Ordinal);
            Assert.Equal(1, media.AudioChannels);
            Assert.Equal(44100, media.AudioSampleRate);
            Assert.Equal(16, media.BitsPerSample);
        }

        [Fact]
        public void ReadStream_WavFixture_MpegIsNull()
        {
            var path = _RequireFixture("minimal-silent.wav");

            var snapshot = MediaPropertiesReader.ReadStream(path);

            Assert.Null(snapshot.Mpeg);
            Assert.Equal(1, snapshot.Media.AudioChannels);
        }

        [Fact]
        public void Read_MissingFile_ThrowsArgumentException()
        {
            var missing = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_missing.mp3");

            var ex = Assert.Throws<ArgumentException>(() => MediaPropertiesReader.Read(missing));
            Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_Directory_ThrowsArgumentException()
        {
            var dir = Path.GetTempPath();

            var ex = Assert.Throws<ArgumentException>(() => MediaPropertiesReader.Read(Path.GetFullPath(dir)));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Read_RelativePath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => MediaPropertiesReader.Read("relative.mp3"));
        }

        [Fact]
        public void Read_UnsupportedTextFile_Throws()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_plain.txt");
            try
            {
                File.WriteAllText(path, "not media");
                Assert.ThrowsAny<Exception>(() => MediaPropertiesReader.Read(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string _RequireFixture(string fileName)
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
            if (!File.Exists(fixturePath))
            {
                throw new InvalidOperationException(
                    $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output.");
            }

            return Path.GetFullPath(fixturePath);
        }
    }
}
