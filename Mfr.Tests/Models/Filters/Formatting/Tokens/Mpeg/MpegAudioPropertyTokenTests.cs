using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Filters.Formatting.Tokens.Mpeg;
using Mfr.Models.RenameList.Fields.Mpeg;
using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Mpeg
{
    /// <summary>
    /// Tests for <c>mp3-*</c> formatter tokens.
    /// </summary>
    public sealed class MpegAudioPropertyTokenTests
    {
        /// <summary>
        /// Verifies catalog rows use the <c>mp3-*</c> prefix under <c>Audio\MP3</c> with no <c>mpeg-*</c> alias.
        /// </summary>
        [Fact]
        public void Catalog_Mp3Prefix_NoMpegAlias()
        {
            var mp3Entries = FormatTokenCatalog
                .Entries.Where(e => e.CanonicalName.StartsWith("mp3-", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(Enum.GetValues<MpegAudioPropertyField>().Length, mp3Entries.Count);
            Assert.All(mp3Entries, e => Assert.Equal("Audio\\MP3", e.GroupPath));

            Assert.DoesNotContain(
                FormatTokenCatalog.Entries,
                e => e.CanonicalName.StartsWith("mpeg-", StringComparison.Ordinal)
            );

            var ex = Assert.Throws<NotSupportedException>(() => FormatStringCompiler.Compile("<mpeg-bitrate>"));
            Assert.Contains("mpeg-bitrate", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Token enum member names that drift from catalog keys must map explicitly.
        /// </summary>
        [Fact]
        public void CatalogPropertyKey_NameDriftArms_MapExplicitly()
        {
            Assert.Equal(
                MpegRenameListFields.Key.VBR,
                MpegPropertyRenameListField.CatalogPropertyKey(MpegAudioPropertyField.Encoding)
            );
            Assert.Equal(
                MpegRenameListFields.Key.Level,
                MpegPropertyRenameListField.CatalogPropertyKey(MpegAudioPropertyField.MpegVer)
            );
            Assert.Equal(
                MpegRenameListFields.Key.DurationSecs,
                MpegPropertyRenameListField.CatalogPropertyKey(MpegAudioPropertyField.DurationSec)
            );
        }

        [Fact]
        public void TryGetFixedField_NameDriftTokens_MapCatalogKeys()
        {
            var encoding = new MpegEncodingToken();
            Assert.True(encoding.TryGetFixedField(out var encodingGroup, out var encodingKey));
            Assert.Equal(MpegRenameListFields.Group, encodingGroup);
            Assert.Equal(MpegRenameListFields.Key.VBR, encodingKey);

            var mpegVer = new MpegVerToken();
            Assert.True(mpegVer.TryGetFixedField(out var verGroup, out var verKey));
            Assert.Equal(MpegRenameListFields.Group, verGroup);
            Assert.Equal(MpegRenameListFields.Key.Level, verKey);

            var durationSec = new MpegDurationSecToken();
            Assert.True(durationSec.TryGetFixedField(out var durationGroup, out var durationKey));
            Assert.Equal(MpegRenameListFields.Group, durationGroup);
            Assert.Equal(MpegRenameListFields.Key.DurationSecs, durationKey);
        }

        private static MediaProperties _MediaWithMpeg(MpegAudioProperties mpeg)
        {
            return new MediaProperties { Mpeg = mpeg };
        }

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
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = _MediaWithMpeg(_SampleMpeg())
            );

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
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = _MediaWithMpeg(_SampleMpeg(isVbr: true))
            );

            Assert.Equal("VBR128", new MpegBitrateToken().Compile(string.Empty)(item));
            Assert.Equal("VBR", new MpegEncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullMpeg_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Media?.Mpeg);

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
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = _MediaWithMpeg(
                    new MpegAudioProperties
                    {
                        IsCopyrighted = false,
                        IsOriginal = true,
                        IsProtected = false,
                    }
                )
            );

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
                Assert.Contains("mp3-bitrate", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededMpeg()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = _MediaWithMpeg(
                    new MpegAudioProperties
                    {
                        Bitrate = 320,
                        IsVbr = true,
                        Layer = 3,
                        Duration = TimeSpan.FromSeconds(61),
                    }
                )
            );

            var filter = new FormatterFilter(
                Target: new FileNameTarget(),
                Options: new FormatterOptions("<mp3-bitrate>_<mp3-layer>_<mp3-duration>")
            );
            filter.Setup();
            filter.Apply(item);

            Assert.Equal("VBR320_III_0:01:01", item.Preview.FileName);
        }

        [Fact]
        public void Compile_ReadsFromDiskWhenNotMarked()
        {
            var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "l3-compl-cut.mp3");
            Assert.True(File.Exists(fixturePath), $"Missing fixture '{fixturePath}'.");

            var fullPath = Path.GetFullPath(fixturePath);
            var directory = Path.GetDirectoryName(fullPath)!;
            var prefix = Path.GetFileNameWithoutExtension(fullPath);
            var extension = FileMeta.ExtensionWithoutDot(fullPath);

            var meta = new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directory,
                fileName: prefix,
                extension: extension,
                fileSize: new FileInfo(fullPath).Length
            );
            meta.AudioTagOverlay.ContainerFormat = AudioContainerFormat.Mpeg;

            var item = new RenameItem(meta);
            Assert.False(item.TagLibLoadAttempted);
            Assert.Null(item.Original.Media);

            var text = new MpegLayerToken().Compile(string.Empty)(item);

            Assert.True(item.TagLibLoadAttempted);
            Assert.NotNull(item.Original.Media);
            Assert.NotNull(item.Original.Media.Mpeg);
            Assert.Equal("III", text);
            Assert.Equal("CBR", new MpegEncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void ClearMediaPropertiesCache_ClearsNestedMpeg()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = new MediaProperties { AudioBitrate = 128, Mpeg = _SampleMpeg() }
            );

            Assert.NotNull(item.Original.Media?.Mpeg);
            item.ClearMediaPropertiesCache();

            Assert.False(item.TagLibLoadAttempted);
            Assert.Null(item.Original.Media);
            Assert.Null(item.Preview.Media);
        }
    }
}
