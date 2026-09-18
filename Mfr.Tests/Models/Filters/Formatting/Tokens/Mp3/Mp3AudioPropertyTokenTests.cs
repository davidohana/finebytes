using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Filters.Formatting.Tokens.Mp3;
using Mfr.Models.RenameList.Fields.Mp3;
using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Mp3
{
    /// <summary>
    /// Tests for <c>mp3-*</c> formatter tokens.
    /// </summary>
    public sealed class Mp3AudioPropertyTokenTests
    {
        /// <summary>
        /// Verifies catalog rows use the <c>mp3-*</c> prefix under <c>Audio\MP3</c> with no <c>mpeg-*</c> alias.
        /// </summary>
        [Fact]
        public void Catalog_Mp3Prefix_NoMpegAlias()
        {
            Assert.Equal("MP3", Mp3RenameListFields.Group);

            var mp3Entries = FormatTokenCatalog
                .Entries.Where(e => e.CanonicalName.StartsWith("mp3-", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(Enum.GetValues<Mp3AudioPropertyField>().Length, mp3Entries.Count);
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
                Mp3RenameListFields.Key.VBR,
                Mp3PropertyRenameListField.CatalogPropertyKey(Mp3AudioPropertyField.Encoding)
            );
            Assert.Equal(
                Mp3RenameListFields.Key.Level,
                Mp3PropertyRenameListField.CatalogPropertyKey(Mp3AudioPropertyField.Ver)
            );
            Assert.Equal(
                Mp3RenameListFields.Key.DurationSecs,
                Mp3PropertyRenameListField.CatalogPropertyKey(Mp3AudioPropertyField.DurationSec)
            );
        }

        [Fact]
        public void TryGetFixedField_NameDriftTokens_MapCatalogKeys()
        {
            var encoding = new Mp3EncodingToken();
            Assert.True(encoding.TryGetFixedField(out var encodingGroup, out var encodingKey));
            Assert.Equal(Mp3RenameListFields.Group, encodingGroup);
            Assert.Equal(Mp3RenameListFields.Key.VBR, encodingKey);

            var mp3Ver = new Mp3VerToken();
            Assert.True(mp3Ver.TryGetFixedField(out var verGroup, out var verKey));
            Assert.Equal(Mp3RenameListFields.Group, verGroup);
            Assert.Equal(Mp3RenameListFields.Key.Level, verKey);

            var durationSec = new Mp3DurationSecToken();
            Assert.True(durationSec.TryGetFixedField(out var durationGroup, out var durationKey));
            Assert.Equal(Mp3RenameListFields.Group, durationGroup);
            Assert.Equal(Mp3RenameListFields.Key.DurationSecs, durationKey);
        }

        private static MediaProperties _MediaWithMp3(Mp3AudioProperties mp3)
        {
            return new MediaProperties { Mp3 = mp3 };
        }

        private static Mp3AudioProperties _SampleMp3(bool isVbr = false)
        {
            return new Mp3AudioProperties
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
                m.Media = _MediaWithMp3(_SampleMp3())
            );

            Assert.Equal("128", new Mp3BitrateToken().Compile(string.Empty)(item));
            Assert.Equal("Yes", new Mp3CopyrightToken().Compile(string.Empty)(item));
            Assert.Equal("0:03:45", new Mp3DurationToken().Compile(string.Empty)(item));
            Assert.Equal("225", new Mp3DurationSecToken().Compile(string.Empty)(item));
            Assert.Equal("CBR", new Mp3EncodingToken().Compile(string.Empty)(item));
            Assert.Equal("44100", new Mp3FrequencyToken().Compile(string.Empty)(item));
            Assert.Equal("III", new Mp3LayerToken().Compile(string.Empty)(item));
            Assert.Equal("1", new Mp3VerToken().Compile(string.Empty)(item));
            Assert.Equal("JointStereo", new Mp3ModeToken().Compile(string.Empty)(item));
            Assert.Equal("No", new Mp3OriginalToken().Compile(string.Empty)(item));
            Assert.Equal("Yes", new Mp3ProtectionToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_VbrBitrate_PrefixesVbr()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = _MediaWithMp3(_SampleMp3(isVbr: true))
            );

            Assert.Equal("VBR128", new Mp3BitrateToken().Compile(string.Empty)(item));
            Assert.Equal("VBR", new Mp3EncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_NullMp3_YieldsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            Assert.Null(item.Original.Media?.Mp3);

            Assert.Equal(string.Empty, new Mp3BitrateToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3CopyrightToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3DurationToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3EncodingToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3LayerToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3VerToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3OriginalToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3ProtectionToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_ZeroBitrateAndDuration_YieldEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = _MediaWithMp3(
                    new Mp3AudioProperties
                    {
                        IsCopyrighted = false,
                        IsOriginal = true,
                        IsProtected = false,
                    }
                )
            );

            Assert.Equal(string.Empty, new Mp3BitrateToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3DurationToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3DurationSecToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3FrequencyToken().Compile(string.Empty)(item));
            Assert.Equal(string.Empty, new Mp3LayerToken().Compile(string.Empty)(item));
            Assert.Equal("No", new Mp3CopyrightToken().Compile(string.Empty)(item));
            Assert.Equal("Yes", new Mp3OriginalToken().Compile(string.Empty)(item));
            Assert.Equal("No", new Mp3ProtectionToken().Compile(string.Empty)(item));
            Assert.Equal("CBR", new Mp3EncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void Compile_WithAnyArgument_Throws()
        {
            var token = new Mp3BitrateToken();
            var item = FilterTestHelpers.CreateRenameItem();

            foreach (var bad in new[] { "0", "1", "x" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(tokenArgs: bad)(item));
                Assert.Contains("mp3-bitrate", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void FormatterFilter_UsesSeededMp3()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = _MediaWithMp3(
                    new Mp3AudioProperties
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

            var text = new Mp3LayerToken().Compile(string.Empty)(item);

            Assert.True(item.TagLibLoadAttempted);
            Assert.NotNull(item.Original.Media);
            Assert.NotNull(item.Original.Media.Mp3);
            Assert.Equal("III", text);
            Assert.Equal("CBR", new Mp3EncodingToken().Compile(string.Empty)(item));
        }

        [Fact]
        public void ClearMediaPropertiesCache_ClearsNestedMp3()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.Media = new MediaProperties { AudioBitrate = 128, Mp3 = _SampleMp3() }
            );

            Assert.NotNull(item.Original.Media?.Mp3);
            item.ClearMediaPropertiesCache();

            Assert.False(item.TagLibLoadAttempted);
            Assert.Null(item.Original.Media);
            Assert.Null(item.Preview.Media);
        }
    }
}
