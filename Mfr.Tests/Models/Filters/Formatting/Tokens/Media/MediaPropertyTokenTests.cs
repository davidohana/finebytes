using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Media;
using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Media
{
    /// <summary>
    /// Tests for <c>media-*</c> formatter tokens.
    /// </summary>
    public sealed class MediaPropertyTokenTests
    {
        private static MediaProperties _SampleMedia()
        {
            return new MediaProperties
            {
                MimeType = "taglib/mp3",
                PossiblyCorrupt = false,
                Duration = TimeSpan.FromSeconds(225),
                MediaTypes = "Audio",
                Description = "MPEG Version 1 Audio, Layer 3",
                AudioBitrate = 128,
                AudioSampleRate = 44100,
                BitsPerSample = 16,
                AudioChannels = 2,
                VideoWidth = 0,
                VideoHeight = 0,
                PhotoWidth = 1920,
                PhotoHeight = 1080,
                PhotoQuality = 85,
            };
        }

        [Fact]
        public void Resolve_SeededFields_FormatPerRules()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Media = _SampleMedia());

            Assert.Equal("taglib/mp3", new MediaMimeToken().Compile(string.Empty)(item));
            Assert.Equal("No", new MediaCorruptToken().Compile(string.Empty)(item));
            Assert.Equal("0:03:45", new MediaDurationToken().Compile(string.Empty)(item));
            Assert.Equal("225", new MediaDurationSecToken().Compile(string.Empty)(item));
            Assert.Equal("Audio", new MediaTypesToken().Compile(string.Empty)(item));
            Assert.Equal("MPEG Version 1 Audio, Layer 3", new MediaDescriptionToken().Compile(string.Empty)(item));
            Assert.Equal("128", new MediaAudioBitrateToken().Compile(string.Empty)(item));
            Assert.Equal("44100", new MediaSampleRateToken().Compile(string.Empty)(item));
            Assert.Equal("16", new MediaBitsPerSampleToken().Compile(string.Empty)(item));
            Assert.Equal("2", new MediaChannelsToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MediaVideoWidthToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MediaVideoHeightToken().Compile(string.Empty)(item));
            Assert.Equal("1920", new MediaPhotoWidthToken().Compile(string.Empty)(item));
            Assert.Equal("1080", new MediaPhotoHeightToken().Compile(string.Empty)(item));
            Assert.Equal("85", new MediaPhotoQualityToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_CorruptTrue_FormatsYes()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Media = new MediaProperties { PossiblyCorrupt = true });

            Assert.Equal("Yes", new MediaCorruptToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_ZeroDurationAndInts_YieldEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Media = new MediaProperties());

            Assert.Equal(string.Empty, new MediaDurationToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MediaDurationSecToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MediaAudioBitrateToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MediaChannelsToken().Compile(string.Empty)(item));
            Assert.Equal("No", new MediaCorruptToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullMedia_YieldsEmpty_ExceptCorruptNeedsLoad()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Media);

            Assert.Equal(string.Empty, new MediaMimeToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MediaDurationToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MediaCorruptToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_LongDuration_UsesTotalHours()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Media = new MediaProperties
                {
                    Duration = TimeSpan.FromHours(25) + TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(2),
                });

            Assert.Equal("25:01:02", new MediaDurationToken().Compile(string.Empty)(item));
            Assert.Equal("90062", new MediaDurationSecToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Compile_WithAnyArgument_Throws()
        {
            var token = new MediaMimeToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("media-mime", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededMedia()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Media = new MediaProperties
                {
                    AudioBitrate = 320,
                    Duration = TimeSpan.FromSeconds(61),
                });

            var filter = new FormatterFilter(
                Target: new FilePrefixTarget(),
                Options: new FormatterOptions("<media-audio-bitrate>_<media-duration>"));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("320_0:01:01", item.Preview.Prefix);
        }

        [Fact]
        public void EnsureMediaPropertiesLoaded_ReadsFromDiskWhenNotMarked()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "minimal-silent.wav");
            Assert.True(File.Exists(fixturePath), $"Missing fixture '{fixturePath}'.");

            var fullPath = Path.GetFullPath(fixturePath);
            var directory = Path.GetDirectoryName(fullPath)!;
            var prefix = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);

            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directory,
                prefix: prefix,
                extension: extension,
                fileSize: new FileInfo(fullPath).Length);
            meta.AudioTagOverlay.ContainerFormat = AudioContainerFormat.Riff;

            var item = new RenameItem(meta);
            item.MarkEmbeddedTagsLoadAttempted();
            Assert.False(item.MediaPropertiesLoadAttempted);
            Assert.Null(item.Original.Media);

            var text = new MediaChannelsToken().Compile(string.Empty)(item);

            Assert.True(item.MediaPropertiesLoadAttempted);
            Assert.NotNull(item.Original.Media);
            Assert.Equal("1", text);
        }
    }
}
