using Mfr.Filters.Formatting;
using Mfr.Models;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests format-specific audio targets (<see cref="Id3v1FieldTarget"/>, <see cref="Id3v2FrameTarget"/>, <see cref="XiphFieldTarget"/>).
    /// </summary>
    public sealed class FormatSpecificAudioTargetFilterTests
    {
        /// <summary>
        /// Verifies a formatter can set ID3v2 <c>TIT2</c> on an MPEG row without touching ID3v1.
        /// </summary>
        [Fact]
        public void Formatter_Id3v2Frame_SetsTit2_OnMpeg()
        {
            var filter = new FormatterFilter(
                new Id3v2FrameTarget("TIT2"),
                new FormatterOptions("FrameTitle"));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Id3v1 = new Id3v1TagData { Title = "TrailerOnly" },
                        Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
                    };
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("TrailerOnly", item.Preview.AudioTagOverlay.Id3v1!.Title);
            Assert.Equal("FrameTitle", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2"));
        }

        /// <summary>
        /// Verifies primary <c>COMM</c> (empty description) is written with default language <c>eng</c>.
        /// </summary>
        [Fact]
        public void Formatter_Id3v2Frame_SetsPrimaryComm()
        {
            var filter = new FormatterFilter(
                new Id3v2FrameTarget("COMM"),
                new FormatterOptions("Primary comment"));
            var item = FilterTestHelpers.CreateRenameItem();

            filter.Setup();
            filter.Apply(item);

            var frame = Assert.Single(
                item.Preview.AudioTagOverlay.Id3v2!.Frames,
                f => f.FrameId == "COMM");
            Assert.Null(frame.Description);
            Assert.Equal("eng", frame.Language);
            Assert.Equal("Primary comment", Assert.Single(frame.TextValues));
        }

        /// <summary>
        /// Verifies a Xiph <c>TITLE</c> key is set on a FLAC-style synthetic row.
        /// </summary>
        [Fact]
        public void Formatter_XiphField_SetsTitle_OnFlac()
        {
            var filter = new FormatterFilter(
                new XiphFieldTarget("title"),
                new FormatterOptions("VorbisTitle"));
            var item = FilterTestHelpers.CreateRenameItem(
                extension: ".flac",
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Xiph = new XiphTagData { Fields = [] },
                    };
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("VorbisTitle", item.Preview.AudioTagOverlay.Semantic().Title);
            Assert.Null(item.Preview.AudioTagOverlay.Id3v2);
        }

        /// <summary>
        /// Verifies an ID3v1 title write leaves ID3v2 alone.
        /// </summary>
        [Fact]
        public void Formatter_Id3v1Field_SetsTitle_OnlyOnId3v1()
        {
            var filter = new FormatterFilter(
                new Id3v1FieldTarget(Id3v1Field.Title),
                new FormatterOptions("V1Title"));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Id3v1 = new Id3v1TagData { Title = "Old" },
                        Id3v2 = new Id3v2TagData
                        {
                            Version = 3,
                            Frames =
                            [
                                new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["FrameStay"] },
                            ],
                        },
                    };
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("V1Title", item.Preview.AudioTagOverlay.Id3v1!.Title);
            Assert.Equal("FrameStay", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2"));
        }

        /// <summary>
        /// Verifies ID3v2 targets on FLAC throw <see cref="NotSupportedException"/>.
        /// </summary>
        [Fact]
        public void Formatter_Id3v2Frame_OnFlac_ThrowsNotSupported()
        {
            var filter = new FormatterFilter(
                new Id3v2FrameTarget("TIT2"),
                new FormatterOptions("Nope"));
            var item = FilterTestHelpers.CreateRenameItem(
                extension: ".flac",
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Xiph = new XiphTagData { Fields = [] },
                    };
                });

            filter.Setup();
            var ex = Assert.Throws<NotSupportedException>(() => filter.Apply(item));
            Assert.Contains("ID3v2", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies Xiph targets on MP3 throw <see cref="NotSupportedException"/>.
        /// </summary>
        [Fact]
        public void Formatter_XiphField_OnMp3_ThrowsNotSupported()
        {
            var filter = new FormatterFilter(
                new XiphFieldTarget("TITLE"),
                new FormatterOptions("Nope"));
            var item = FilterTestHelpers.CreateRenameItem();

            filter.Setup();
            var ex = Assert.Throws<NotSupportedException>(() => filter.Apply(item));
            Assert.Contains("Xiph", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Empty template clears a previously set ID3v2 frame instance.
        /// </summary>
        [Fact]
        public void Formatter_Id3v2Frame_EmptyTemplate_ClearsFrame()
        {
            var filter = new FormatterFilter(
                new Id3v2FrameTarget("TIT2"),
                new FormatterOptions(string.Empty));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Id3v2 = new Id3v2TagData
                        {
                            Version = 3,
                            Frames =
                            [
                                new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["Gone"] },
                                new Id3v2ModeledFrame { FrameId = "TALB", TextValues = ["Keep"] },
                            ],
                        },
                    };
                });

            filter.Setup();
            filter.Apply(item);

            Assert.DoesNotContain(item.Preview.AudioTagOverlay.Id3v2!.Frames, f => f.FrameId == "TIT2");
            Assert.Contains(item.Preview.AudioTagOverlay.Id3v2.Frames, f => f.FrameId == "TALB");
        }

        /// <summary>
        /// Writing v2.4-only <c>TDRC</c> into a v2.3 overlay throws <see cref="NotSupportedException"/>.
        /// </summary>
        [Fact]
        public void Formatter_Id3v2Frame_TdrcOnV23_ThrowsNotSupported()
        {
            var filter = new FormatterFilter(
                new Id3v2FrameTarget("TDRC"),
                new FormatterOptions("2020"));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
                    };
                });

            filter.Setup();
            var ex = Assert.Throws<NotSupportedException>(() => filter.Apply(item));
            Assert.Contains("TDRC", ex.Message, StringComparison.Ordinal);
            Assert.Contains("2.4", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Writing <c>TDRC</c> into a v2.4 overlay succeeds.
        /// </summary>
        [Fact]
        public void Formatter_Id3v2Frame_TdrcOnV24_Succeeds()
        {
            var filter = new FormatterFilter(
                new Id3v2FrameTarget("TDRC"),
                new FormatterOptions("2020"));
            var item = FilterTestHelpers.CreateRenameItem(
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Id3v2 = new Id3v2TagData { Version = 4, Frames = [] },
                    };
                });

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("2020", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TDRC"));
            Assert.Equal(4, item.Preview.AudioTagOverlay.Id3v2!.Version);
        }

        /// <summary>
        /// Clearing a v2.4-only frame on a v2.3 tag does not throw (removal is always allowed).
        /// </summary>
        [Fact]
        public void SetId3v2FrameString_ClearTdrcOnV23_Succeeds()
        {
            var overlay = new AudioTagOverlay
            {
                Id3v2 = new Id3v2TagData
                {
                    Version = 3,
                    Frames =
                    [
                        new Id3v2ModeledFrame { FrameId = "TDRC", TextValues = ["2019"] },
                        new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["Keep"] },
                    ],
                },
            };

            AudioOverlayBlockFieldIo.SetId3v2FrameString(overlay, "TDRC", string.Empty);

            Assert.DoesNotContain(overlay.Id3v2.Frames, f => f.FrameId == "TDRC");
            Assert.Equal("Keep", AudioOverlayBlockFieldIo.GetId3v2FrameString(overlay, "TIT2"));
        }
    }
}
