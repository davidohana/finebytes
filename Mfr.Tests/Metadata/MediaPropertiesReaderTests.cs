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
        }

        [Fact]
        public void Read_Mp3Fixture_PopulatesNestedMp3()
        {
            var path = _RequireFixture("l3-compl-cut.mp3");

            var media = MediaPropertiesReader.Read(path);

            Assert.NotNull(media.Mp3);
            Assert.Equal(3, media.Mp3.Layer);
            Assert.False(media.Mp3.IsVbr);
            Assert.Equal("1", media.Mp3.MpegVersion);
            Assert.True(media.Mp3.Bitrate > 0);
            Assert.True(media.Mp3.SampleRate > 0);
            Assert.False(string.IsNullOrEmpty(media.Mp3.ChannelMode));
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
        public void Read_WavFixture_Mp3IsNull()
        {
            var path = _RequireFixture("minimal-silent.wav");

            var media = MediaPropertiesReader.Read(path);

            Assert.Null(media.Mp3);
            Assert.Equal(1, media.AudioChannels);
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

        /// <summary>
        /// Verifies TagLibFileAccess survives files TagLib opens without stream properties.
        /// </summary>
        [Fact]
        public void Read_InvalidWavPayload_DoesNotThrowNullReference()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
            try
            {
                File.WriteAllText(path, "not audio");

                var ex = Record.Exception(() => TagLibFileAccess.Read(path));

                Assert.NotNull(ex);
                Assert.IsNotType<NullReferenceException>(ex);
            }
            finally
            {
                File.Delete(path);
            }
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
                    $"Missing fixture '{fixturePath}'. Run build so Fixtures copy to output."
                );
            }

            return Path.GetFullPath(fixturePath);
        }
    }
}
