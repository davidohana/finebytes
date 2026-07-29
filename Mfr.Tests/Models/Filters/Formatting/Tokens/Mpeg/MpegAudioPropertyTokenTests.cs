using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Mpeg;
using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Mpeg
{
    /// <summary>
    /// Tests for <c>mpeg-*</c> formatter tokens.
    /// </summary>
    public sealed class MpegAudioPropertyTokenTests
    {
        private static MpegAudioProperties _SampleMpeg(bool isVbr = false)
        {
            return new MpegAudioProperties
            {
                Bitrate = 128,
                IsCopyrighted = true,
                Duration = TimeSpan.FromSeconds(225),
                IsVbr = isVbr,
                SampleRate = 44100,
                Layer = 3,
                MpegVersion = "1",
                ChannelMode = "JointStereo",
                IsOriginal = false,
                IsProtected = true,
            };
        }

        [Fact]
        public void Resolve_SeededFields_FormatPerRules()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m => m.Mpeg = _SampleMpeg());

            Assert.Equal("128", new MpegBitrateToken().Compile(string.Empty)(item));
            Assert.Equal("Yes", new MpegCopyrightToken().Compile(string.Empty)(item));
            Assert.Equal("0:03:45", new MpegDurationToken().Compile(string.Empty)(item));
            Assert.Equal("225", new MpegDurationSecToken().Compile(string.Empty)(item));
            Assert.Equal("CBR", new MpegEncodingToken().Compile(string.Empty)(item));
            Assert.Equal("44100", new MpegFrequencyToken().Compile(string.Empty)(item));
            Assert.Equal("III", new MpegLayerToken().Compile(string.Empty)(item));
            Assert.Equal("1", new MpegVerToken().Compile(string.Empty)(item));
            Assert.Equal("JointStereo", new MpegModeToken().Compile(string.Empty)(item));
            Assert.Equal("No", new MpegOriginalToken().Compile(string.Empty)(item));
            Assert.Equal("Yes", new MpegProtectionToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_VbrBitrate_PrefixesVbr()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Mpeg = _SampleMpeg(isVbr: true));

            Assert.Equal("VBR128", new MpegBitrateToken().Compile(string.Empty)(item));
            Assert.Equal("VBR", new MpegEncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullMpeg_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Mpeg);

            Assert.Equal(string.Empty, new MpegBitrateToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegCopyrightToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegDurationToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegEncodingToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegLayerToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegVerToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegOriginalToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegProtectionToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_ZeroBitrateAndDuration_YieldEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Mpeg = new MpegAudioProperties
                {
                    IsCopyrighted = false,
                    IsOriginal = true,
                    IsProtected = false,
                });

            Assert.Equal(string.Empty, new MpegBitrateToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegDurationToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegDurationSecToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegFrequencyToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new MpegLayerToken().Compile(string.Empty)(item));
            Assert.Equal("No", new MpegCopyrightToken().Compile(string.Empty)(item));
            Assert.Equal("Yes", new MpegOriginalToken().Compile(string.Empty)(item));
            Assert.Equal("No", new MpegProtectionToken().Compile(string.Empty)(item));
            Assert.Equal("CBR", new MpegEncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Compile_WithAnyArgument_Throws()
        {
            var token = new MpegBitrateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("mpeg-bitrate", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededMpeg()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m => m.Mpeg = new MpegAudioProperties
                {
                    Bitrate = 320,
                    IsVbr = true,
                    Layer = 3,
                    Duration = TimeSpan.FromSeconds(61),
                });

            var filter = new FormatterFilter(
                Target: new FilePrefixTarget(),
                Options: new FormatterOptions("<mpeg-bitrate>_<mpeg-layer>_<mpeg-duration>"));
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("VBR320_III_0:01:01", item.Preview.Prefix);
        }

        [Fact]
        public void EnsureMpegAudioPropertiesLoaded_ReadsFromDiskWhenNotMarked()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "l3-compl-cut.mp3");
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
            meta.AudioTagOverlay.ContainerFormat = AudioContainerFormat.Mpeg;

            var item = new RenameItem(meta);
            item.MarkEmbeddedTagsLoadAttempted();
            Assert.False(item.MediaPropertiesLoadAttempted);
            Assert.Null(item.Original.Mpeg);
            Assert.Null(item.Original.Media);

            var text = new MpegLayerToken().Compile(string.Empty)(item);

            Assert.True(item.MediaPropertiesLoadAttempted);
            Assert.NotNull(item.Original.Media);
            Assert.NotNull(item.Original.Mpeg);
            Assert.Equal("III", text);
            Assert.Equal("CBR", new MpegEncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void ClearMediaPropertiesCache_ClearsMpeg()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.Media = new MediaProperties { AudioBitrate = 128 };
                    m.Mpeg = _SampleMpeg();
                });

            Assert.NotNull(item.Original.Mpeg);
            item.ClearMediaPropertiesCache();

            Assert.False(item.MediaPropertiesLoadAttempted);
            Assert.Null(item.Original.Media);
            Assert.Null(item.Original.Mpeg);
            Assert.Null(item.Preview.Media);
            Assert.Null(item.Preview.Mpeg);
        }
    }
}
