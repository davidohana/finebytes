using System.Text.Json;
using Mfr.Engine;
using Mfr.Filters.Audio;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.Xiph;

namespace Mfr.Tests.Models.Filters.Audio
{
    /// <summary>
    /// Tests for <see cref="Id3v2FieldSetterFilter"/>.
    /// </summary>
    public sealed class Id3v2FieldSetterFilterTests
    {
        /// <summary>
        /// Verifies default <c>onlyIfEmpty: false</c> overwrites an existing <c>TIT2</c> frame.
        /// </summary>
        [Fact]
        public void Apply_Tit2_AlwaysOverwrites()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.AudioTagOverlay = new AudioTagOverlay
                {
                    Id3v2 = new Id3v2TagData
                    {
                        Version = 3,
                        Frames = [new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["Old"] }],
                    },
                };
            });
            var filter = new Id3v2FieldSetterFilter(new Id3v2FieldSetterOptions(FrameId: "TIT2", Text: "New"));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("New", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2"));
        }

        /// <summary>
        /// Verifies <c>onlyIfEmpty</c> leaves a non-empty frame unchanged.
        /// </summary>
        [Fact]
        public void OnlyIfEmpty_LeavesNonEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.AudioTagOverlay = new AudioTagOverlay
                {
                    Id3v2 = new Id3v2TagData
                    {
                        Version = 3,
                        Frames = [new Id3v2ModeledFrame { FrameId = "TIT2", TextValues = ["Kept"] }],
                    },
                };
            });
            var filter = new Id3v2FieldSetterFilter(
                new Id3v2FieldSetterOptions(FrameId: "TIT2", Text: "Fill", OnlyIfEmpty: true)
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Kept", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2"));
        }

        /// <summary>
        /// Verifies <c>onlyIfEmpty</c> fills when the frame is absent.
        /// </summary>
        [Fact]
        public void OnlyIfEmpty_FillsWhenEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.AudioTagOverlay = new AudioTagOverlay
                {
                    Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
                };
            });
            var filter = new Id3v2FieldSetterFilter(
                new Id3v2FieldSetterOptions(FrameId: "TIT2", Text: "Fill", OnlyIfEmpty: true)
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Fill", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2"));
        }

        /// <summary>
        /// Verifies a missing ID3v2 block is created and ID3v1 is left alone.
        /// </summary>
        [Fact]
        public void Apply_CreatesId3v2_LeavesId3v1Alone()
        {
            var item = FilterTestHelpers.CreateRenameItem(configureOriginal: m =>
            {
                m.AudioTagOverlay = new AudioTagOverlay
                {
                    Id3v1 = new Id3v1TagData { Title = "TrailerOnly" },
                    Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
                };
            });
            item.Original.AudioTagOverlay.Id3v2 = null;
            item.Preview.AudioTagOverlay.Id3v2 = null;

            var filter = new Id3v2FieldSetterFilter(new Id3v2FieldSetterOptions(FrameId: "TIT2", Text: "FrameTitle"));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("TrailerOnly", item.Preview.AudioTagOverlay.Id3v1!.Title);
            Assert.Equal(3, item.Preview.AudioTagOverlay.Id3v2!.Version);
            Assert.Equal(
                "FrameTitle",
                AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2")
            );
        }

        /// <summary>
        /// Verifies <c>text</c> may contain formatter tokens.
        /// </summary>
        [Fact]
        public void Apply_TextWithFormatToken_Expands()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "MySong");
            var filter = new Id3v2FieldSetterFilter(new Id3v2FieldSetterOptions(FrameId: "TIT2", Text: "<file-name>"));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("MySong", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2"));
        }

        /// <summary>
        /// Verifies primary <c>COMM</c> is written with default language <c>eng</c>.
        /// </summary>
        [Fact]
        public void Apply_PrimaryComm_SetsEngLanguage()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var filter = new Id3v2FieldSetterFilter(
                new Id3v2FieldSetterOptions(FrameId: "COMM", Text: "Primary comment")
            );

            filter.Setup();
            filter.Apply(item);

            var frame = Assert.Single(item.Preview.AudioTagOverlay.Id3v2!.Frames, f => f.FrameId == "COMM");
            Assert.Null(frame.Description);
            Assert.Equal("eng", frame.Language);
            Assert.Equal("Primary comment", Assert.Single(frame.TextValues));
        }

        /// <summary>
        /// Verifies ID3v2 field setter on FLAC throws <see cref="NotSupportedException"/>.
        /// </summary>
        [Fact]
        public void Apply_OnFlac_ThrowsNotSupported()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                extension: ".flac",
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay { Xiph = new XiphTagData { Fields = [] } };
                }
            );
            var filter = new Id3v2FieldSetterFilter(new Id3v2FieldSetterOptions(FrameId: "TIT2", Text: "Nope"));

            filter.Setup();
            var ex = Assert.Throws<NotSupportedException>(() => filter.Apply(item));
            Assert.Contains("ID3v2", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies empty <c>frameId</c> fails at setup.
        /// </summary>
        [Fact]
        public void Setup_EmptyFrameId_Throws()
        {
            var filter = new Id3v2FieldSetterFilter(new Id3v2FieldSetterOptions(FrameId: "  ", Text: "X"));
            var ex = Assert.Throws<ArgumentException>(filter.Setup);
            Assert.Contains("frameId", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies preset JSON deserializes this filter type.
        /// </summary>
        [Fact]
        public void JsonDeserialize_Roundtrip()
        {
            var json = /*lang=json,strict*/
                """
                {
                  "type": "Id3v2FieldSetter",
                  "options": {
                    "frameId": "tit2",
                    "text": "<file-name>",
                    "onlyIfEmpty": true
                  }
                }
                """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            var typed = Assert.IsType<Id3v2FieldSetterFilter>(filter);
            Assert.Equal("tit2", typed.Options.FrameId);
            Assert.True(typed.Options.OnlyIfEmpty);
            typed.Setup();

            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "P",
                configureOriginal: m =>
                {
                    m.AudioTagOverlay = new AudioTagOverlay
                    {
                        Id3v2 = new Id3v2TagData { Version = 3, Frames = [] },
                    };
                }
            );
            typed.Apply(item);
            Assert.Equal("P", AudioOverlayBlockFieldIo.GetId3v2FrameString(item.Preview.AudioTagOverlay, "TIT2"));
        }
    }
}
