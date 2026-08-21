using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Audio;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Tests for the MFR7-compatible <c>id3v2</c> formatter token.
    /// </summary>
    public sealed class Id3v2TokenTests
    {
        [Fact]
        public void Resolve_Tit2_ReturnsPreviewValue()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["Hello"] }
                )
            );

            Assert.Equal("Hello", token.Compile("TIT2")(item));
            Assert.Contains("id3v2", token.Names);
        }

        [Fact]
        public void Resolve_BareTxxx_ReturnsFirstOfMultiple()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    new Id3v2ModeledFrame
                    {
                        FrameId = "TXXX",
                        Description = "replaygain",
                        TextValues = ["-6.5 dB"],
                    },
                    new Id3v2ModeledFrame
                    {
                        FrameId = "TXXX",
                        Description = "catalog",
                        TextValues = ["ABC-123"],
                    }
                )
            );

            Assert.Equal("-6.5 dB", token.Compile("TXXX")(item));
        }

        [Fact]
        public void Resolve_TxxxWithDescription_ReturnsMatchingOnly()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    new Id3v2ModeledFrame
                    {
                        FrameId = "TXXX",
                        Description = "replaygain",
                        TextValues = ["-6.5 dB"],
                    },
                    new Id3v2ModeledFrame
                    {
                        FrameId = "TXXX",
                        Description = "catalog",
                        TextValues = ["ABC-123"],
                    }
                )
            );

            Assert.Equal("ABC-123", token.Compile("TXXX:catalog")(item));
            Assert.Equal(string.Empty, token.Compile("TXXX:missing")(item));
        }

        [Fact]
        public void Resolve_TxxxMultiValue_JoinsWithSemicolon()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    new Id3v2ModeledFrame
                    {
                        FrameId = "TXXX",
                        Description = "tags",
                        TextValues = ["a", "b"],
                    }
                )
            );

            Assert.Equal("a; b", token.Compile("TXXX:tags")(item));
        }

        [Fact]
        public void Resolve_MissingFrame_YieldsEmpty()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    new Id3v2ModeledFrame { FrameId = "TALB", TextValues = ["OnlyAlbum"] }
                )
            );

            Assert.Equal(string.Empty, token.Compile("TIT2")(item));
            Assert.Equal(string.Empty, token.Compile("TXXX")(item));
        }

        [Fact]
        public void Resolve_NullId3v2Block_YieldsEmpty()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                // Non-null Xiph keeps EnsureSyntheticAudioOverlayWhenTagless from replacing the overlay.
                m.AudioTagOverlay = new AudioTagOverlay
                {
                    ContainerFormat = AudioContainerFormat.Mpeg,
                    Id3v2 = null,
                    Xiph = new XiphTagData { Fields = [] },
                };
            });

            Assert.Equal(string.Empty, token.Compile("TIT2")(item));
            Assert.Equal(string.Empty, token.Compile("TXXX")(item));
        }

        [Fact]
        public void Resolve_Version_FormatsMinorAsTwoDotN()
        {
            var token = new Id3v2VersionToken();
            var v23 = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    version: 3,
                    new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["A"] }
                )
            );
            var v24 = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    version: 4,
                    new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["B"] }
                )
            );

            Assert.Equal("2.3", token.Compile(string.Empty)(v23));
            Assert.Equal("2.4", token.Compile(string.Empty)(v24));
            Assert.Contains("id3v2-version", token.Names);
        }

        [Fact]
        public void Resolve_Version_NullBlock_YieldsEmpty()
        {
            var token = new Id3v2VersionToken();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = new AudioTagOverlay
                {
                    ContainerFormat = AudioContainerFormat.Mpeg,
                    Id3v2 = null,
                    Xiph = new XiphTagData { Fields = [] },
                }
            );

            Assert.Equal(string.Empty, token.Compile(string.Empty)(item));
        }

        [Fact]
        public void Resolve_Version_WithArgument_Throws()
        {
            var token = new Id3v2VersionToken();
            var ex = Assert.Throws<ArgumentException>(() => token.Compile("0")(FilterTestHelpers.CreateRenameItem()));
            Assert.Contains("id3v2-version", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Resolve_PrefersPreviewOverlayOverOriginal()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    new Id3v2ModeledFrame { FrameId = "TALB", TextValues = ["OrigAlbum"] }
                )
            );

            item.Preview.AudioTagOverlay = _OverlayWithFrames(
                new Id3v2ModeledFrame { FrameId = "TALB", TextValues = ["PrevAlbum"] }
            );

            Assert.Equal("PrevAlbum", token.Compile("TALB")(item));
        }

        [Fact]
        public void Resolve_PrimaryComm_OmitsDescription()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(
                    new Id3v2ModeledFrame
                    {
                        FrameId = "COMM",
                        Language = "eng",
                        Description = null,
                        TextValues = ["Primary"],
                    },
                    new Id3v2ModeledFrame
                    {
                        FrameId = "COMM",
                        Language = "eng",
                        Description = "other",
                        TextValues = ["Secondary"],
                    }
                )
            );

            Assert.Equal("Primary", token.Compile("COMM")(item));
            Assert.Equal("Secondary", token.Compile("COMM:other")(item));
        }

        [Fact]
        public void Compile_MissingOrWhitespaceArgs_Throws()
        {
            var token = new Id3v2Token();
            foreach (var bad in new[] { "", "   ", "\t" })
            {
                var ex = Assert.Throws<ArgumentException>(() => token.Compile(bad));
                Assert.Contains("id3v2", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Compile_SingletonWithDescriptionSuffix_Throws()
        {
            var token = new Id3v2Token();
            var ex = Assert.Throws<ArgumentException>(() => token.Compile("TIT2:extra"));
            Assert.Contains("TIT2", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Compile_CaseInsensitiveFrameId()
        {
            var token = new Id3v2Token();
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
                m.AudioTagOverlay = _OverlayWithFrames(new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["Hi"] })
            );

            Assert.Equal("Hi", token.Compile("tit2")(item));
        }

        [Fact]
        public void Apply_FormatterUsesId3v2Token()
        {
            var filter = new FormatterFilter(
                new FilePrefixTarget(),
                new FormatterOptions("<id3v2:TXXX:catalog>-<id3v2:TIT2>")
            );
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                configureOriginal: m =>
                    m.AudioTagOverlay = _OverlayWithFrames(
                        new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["Title"] },
                        new Id3v2ModeledFrame
                        {
                            FrameId = "TXXX",
                            Description = "catalog",
                            TextValues = ["C1"],
                        }
                    )
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("C1-Title", item.Preview.Prefix);
        }

        private static AudioTagOverlay _OverlayWithFrames(params Id3v2ModeledFrame[] frames)
        {
            return _OverlayWithFrames(version: 3, frames);
        }

        private static AudioTagOverlay _OverlayWithFrames(byte version, params Id3v2ModeledFrame[] frames)
        {
            return new AudioTagOverlay
            {
                ContainerFormat = AudioContainerFormat.Mpeg,
                Id3v2 = new Id3v2TagData { Version = version, Frames = [.. frames] },
            };
        }
    }
}
