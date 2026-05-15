using System.Text.Json;
using Mfr.Core;
using Mfr.Filters.Audio;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Audio
{
    /// <summary>
    /// Tests for <see cref="AudioTagSetterFilter"/>.
    /// </summary>
    public sealed class AudioTagSetterFilterTests
    {
        private static readonly DateTime s_Baseline = new(2024, 6, 1, 12, 30, 45, DateTimeKind.Unspecified);

        private static RenameItem _CreateAudioItem(
            int renameListIndex = 0,
            Action<FileMeta>? configureOriginal = null,
            string prefix = "song",
            string extension = ".mp3")
        {
            var meta = new FileMeta(
                renameListIndex,
                inFolderIndex: 0,
                directoryPath: @"C:\Music\Album",
                prefix: prefix,
                extension: extension,
                attributes: FileAttributes.Normal,
                creationTime: s_Baseline,
                lastWriteTime: s_Baseline,
                lastAccessTime: s_Baseline,
                fileSize: 0,
                renameListTotalCount: Math.Max(renameListIndex + 1, 1),
                renameListFolderSiblingCount: 1);

            configureOriginal?.Invoke(meta);
            return new RenameItem(meta, FilterTestHelpers.AudioTagReaderSnapshot(meta));
        }

        private static RenameItem _CreateDirectoryItem()
        {
            var meta = new FileMeta(
                0,
                0,
                @"C:\Music",
                "AlbumDir",
                string.Empty,
                FileAttributes.Directory,
                s_Baseline,
                s_Baseline,
                s_Baseline);

            return new RenameItem(meta);
        }

        /// <summary>
        /// Verifies default <c>onlyIfEmpty: false</c> overwrites an existing title.
        /// </summary>
        [Fact]
        public void Apply_Title_AlwaysOverwrites()
        {
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay.Title = "Old");
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "New")));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("New", item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies performer <c>text</c> may list several names separated by <c>;</c> on the preview overlay
        /// (normalized to separate TagLib values on save via <see cref="Mfr.Metadata.AudioTagPersistence"/>).
        /// </summary>
        [Fact]
        public void Apply_Performers_SemicolonSeparated_SetsJoinedPreviewString()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Performers: new AudioTagStringFieldOptions(Text: "Alice ; Bob")));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Alice ; Bob", item.Preview.AudioTagOverlay.Performers);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> leaves a non-empty title unchanged.
        /// </summary>
        [Fact]
        public void IfEmpty_Title_LeavesNonEmpty()
        {
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay.Title = "Kept");
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Title: new AudioTagStringFieldOptions(Text: "Other", OnlyIfEmpty: true)));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Kept", item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> sets title when it was empty.
        /// </summary>
        [Fact]
        public void IfEmpty_Title_FillsWhenEmpty()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Title: new AudioTagStringFieldOptions(Text: "Filled", OnlyIfEmpty: true)));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Filled", item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies omitting <c>title</c> on options does not change the title.
        /// </summary>
        [Fact]
        public void OmittedTitle_LeavesTitleUnchanged()
        {
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay.Title = "Stay");
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions());

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Stay", item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies formatter templates are compiled when <c>text</c> contains a qualifying <c>&lt;...&gt;</c> span.
        /// </summary>
        [Fact]
        public void Title_TemplateSpan_CompilesFileNameToken()
        {
            var item = _CreateAudioItem(prefix: "TrackNine");
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Title: new AudioTagStringFieldOptions(Text: "<file-name>")));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("TrackNine", item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies unbalanced or non-token <c>&lt;</c> spans leave <c>text</c> literal.
        /// </summary>
        [Fact]
        public void Title_NoQualifyingToken_LiteralText()
        {
            var item = _CreateAudioItem();
            var literal = "Love < Hate";
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Title: new AudioTagStringFieldOptions(Text: literal)));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(literal, item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies track auto-increment uses <see cref="FileMeta.RenameListIndex"/>.
        /// </summary>
        [Fact]
        public void Track_AutoIncrement_AddsRenameListIndex()
        {
            var item = _CreateAudioItem(renameListIndex: 4);
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Track: new AudioTagStringFieldOptions(Text: "10"),
                TrackAutoIncrement: true));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(14u, item.Preview.AudioTagOverlay.Track);
        }

        /// <summary>
        /// Verifies track numbers are clamped to 255.
        /// </summary>
        [Fact]
        public void Track_AutoIncrement_ClampedTo255()
        {
            var item = _CreateAudioItem(renameListIndex: 10);
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Track: new AudioTagStringFieldOptions(Text: "250"),
                TrackAutoIncrement: true));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(255u, item.Preview.AudioTagOverlay.Track);
        }

        /// <summary>
        /// Verifies track value 0 clears the overlay track.
        /// </summary>
        [Fact]
        public void Track_ZeroWithoutIncrement_Clears()
        {
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay.Track = 7);
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Track: new AudioTagStringFieldOptions(Text: "0")));

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Track);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> does not replace an existing track.
        /// </summary>
        [Fact]
        public void Track_IfEmpty_KeepsExisting()
        {
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay.Track = 3);
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Track: new AudioTagStringFieldOptions(Text: "9", OnlyIfEmpty: true)));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(3u, item.Preview.AudioTagOverlay.Track);
        }

        /// <summary>
        /// Verifies invalid track <c>text</c> throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Track_Text_NonInteger_Throws_FormatException()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Track: new AudioTagStringFieldOptions(Text: "nope")));

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("0-255", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies track base above 255 throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Track_Text_BaseAbove255_Throws_FormatException()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Track: new AudioTagStringFieldOptions(Text: "256")));

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("255", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies year clears when value is 0.
        /// </summary>
        [Fact]
        public void Year_Zero_Clears()
        {
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay.Year = 1999);
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Year: new AudioTagStringFieldOptions(Text: "0")));

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Year);
        }

        /// <summary>
        /// Verifies year above 9999 fails preview with <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Year_Above9999_Throws_FormatException()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Year: new AudioTagStringFieldOptions(Text: "12000")));

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("9999", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies year <c>text</c> without templates is parsed as an integer.
        /// </summary>
        [Fact]
        public void Year_TextLiteral_ParsesInteger()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Year: new AudioTagStringFieldOptions(Text: "2005")));

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(2005u, item.Preview.AudioTagOverlay.Year);
        }

        /// <summary>
        /// Verifies invalid literal year <c>text</c> throws.
        /// </summary>
        [Fact]
        public void Year_TextLiteral_NonInteger_Throws()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Year: new AudioTagStringFieldOptions(Text: "nope")));

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("1-9999", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies directory rows cannot load tags and <see cref="AudioTagSetterFilter"/> apply throws.
        /// </summary>
        [Fact]
        public void DirectoryItem_Apply_ThrowsInvalidOperation()
        {
            var item = _CreateDirectoryItem();
            item.Preview.AudioTagOverlay.Title = "PreviewOnly";

            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions(
                Title: new AudioTagStringFieldOptions(Text: "X")));

            filter.Setup();
            var ex = Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("PreviewOnly", item.Preview.AudioTagOverlay.Title);
        }

        /// <summary>
        /// Verifies preset JSON deserializes this filter type.
        /// </summary>
        [Fact]
        public void JsonDeserialize_Roundtrip()
        {
            var json = /*lang=json,strict*/ """
            {
              "type": "AudioTagSetter",
              "options": {
                "title": {
                  "text": "<file-name>"
                },
                "year": {
                  "text": "2004",
                  "onlyIfEmpty": true
                },
                "track": {
                  "text": "1"
                },
                "trackAutoIncrement": true
              }
            }
            """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            var typed = Assert.IsType<AudioTagSetterFilter>(filter);
            Assert.Null(typed.Options.Genre);
            Assert.True(typed.Options.Year!.OnlyIfEmpty);
            Assert.True(typed.Options.TrackAutoIncrement);
            typed.Setup();

            var item = _CreateAudioItem(renameListIndex: 2, prefix: "P");
            typed.Apply(item);
            Assert.Equal("P", item.Preview.AudioTagOverlay.Title);
            Assert.Equal(2004u, item.Preview.AudioTagOverlay.Year);
            Assert.Equal(3u, item.Preview.AudioTagOverlay.Track);
        }
    }
}
