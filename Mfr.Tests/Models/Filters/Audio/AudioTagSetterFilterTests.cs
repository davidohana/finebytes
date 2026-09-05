using System.Text.Json;
using Mfr.Filters.Audio;
using Mfr.Metadata;
using Mfr.Models.Tags.Id3v1;

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
            string extension = ".mp3"
        )
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
                renameListFolderSiblingCount: 1
            );

            configureOriginal?.Invoke(meta);
            FilterTestHelpers.EnsureSyntheticAudioOverlayWhenTagless(meta);
            var item = new RenameItem(meta);
            item.MarkTagLibLoadAttempted();
            return item;
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
                s_Baseline
            );

            return new RenameItem(meta);
        }

        /// <summary>
        /// Verifies default <c>onlyIfEmpty: false</c> overwrites an existing title.
        /// </summary>
        [Fact]
        public void Apply_Title_AlwaysOverwrites()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Old")
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "New"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("New", item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies performer <c>text</c> may list several names separated by <c>;</c> on the preview overlay
        /// (normalized to separate TagLib values on save via <see cref="AudioTagPersistence"/>).
        /// </summary>
        [Fact]
        public void Apply_Performers_SemicolonSeparated_SetsJoinedPreviewString()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Performers: new AudioTagStringFieldOptions(Text: "Alice ; Bob"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Alice; Bob", item.Preview.AudioTagOverlay.Semantic().Performers);
        }

        /// <summary>
        /// Verifies empty genre text clears the field and does not read back as Blues (ID3v1 index 0).
        /// </summary>
        [Fact]
        public void Apply_Genre_EmptyText_ClearsWithoutBecomingBlues()
        {
            var overlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Song", genre: "Rock");
            overlay.Id3v1 = new Id3v1TagData { Title = "Song", Genre = Id3v1Genres.AudioToIndex("Rock") };
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay = overlay);
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Genre: new AudioTagStringFieldOptions(Text: string.Empty))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Genre);
            Assert.Null(item.Preview.AudioTagOverlay.Id3v1!.Genre);
        }

        /// <summary>
        /// Verifies setting Blues stores ID3v1 index 0 and still projects as Blues (not treated as empty).
        /// </summary>
        [Fact]
        public void Apply_Genre_Blues_PreservesId3v1IndexZero()
        {
            var overlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Song");
            overlay.Id3v1 = new Id3v1TagData { Title = "Song" };
            var item = _CreateAudioItem(configureOriginal: m => m.AudioTagOverlay = overlay);
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Genre: new AudioTagStringFieldOptions(Text: "Blues"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Blues", item.Preview.AudioTagOverlay.Semantic().Genre);
            Assert.Equal((byte)0, item.Preview.AudioTagOverlay.Id3v1!.Genre);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> leaves a non-empty title unchanged.
        /// </summary>
        [Fact]
        public void IfEmpty_Title_LeavesNonEmpty()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Kept")
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "Other", OnlyIfEmpty: true))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Kept", item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> sets title when it was empty.
        /// </summary>
        [Fact]
        public void IfEmpty_Title_FillsWhenEmpty()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "Filled", OnlyIfEmpty: true))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Filled", item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies omitting <c>title</c> on options does not change the title.
        /// </summary>
        [Fact]
        public void OmittedTitle_LeavesTitleUnchanged()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "Stay")
            );
            var filter = new AudioTagSetterFilter(new AudioTagSetterOptions());

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Stay", item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies formatter templates are compiled when <c>text</c> contains a qualifying <c>&lt;...&gt;</c> span.
        /// </summary>
        [Fact]
        public void Title_TemplateSpan_CompilesFileNameToken()
        {
            var item = _CreateAudioItem(prefix: "TrackNine");
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "<file-name>"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("TrackNine", item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies unbalanced or non-token <c>&lt;</c> spans leave <c>text</c> literal.
        /// </summary>
        [Fact]
        public void Title_NoQualifyingToken_LiteralText()
        {
            var item = _CreateAudioItem();
            var literal = "Love < Hate";
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: literal))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(literal, item.Preview.AudioTagOverlay.Semantic().Title);
        }

        /// <summary>
        /// Verifies track auto-increment uses <see cref="FileMeta.RenameListIndex"/>.
        /// </summary>
        [Fact]
        public void Track_AutoIncrement_AddsRenameListIndex()
        {
            var item = _CreateAudioItem(renameListIndex: 4);
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "10"), TrackAutoIncrement: true)
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(14u, item.Preview.AudioTagOverlay.Semantic().Track);
        }

        /// <summary>
        /// Verifies track numbers are clamped to 255.
        /// </summary>
        [Fact]
        public void Track_AutoIncrement_ClampedTo255()
        {
            var item = _CreateAudioItem(renameListIndex: 10);
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "250"), TrackAutoIncrement: true)
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(255u, item.Preview.AudioTagOverlay.Semantic().Track);
        }

        /// <summary>
        /// Verifies <c>track.text</c> with a formatter span expands and parses like a literal track integer.
        /// </summary>
        [Fact]
        public void Track_TemplateSpan_CompilesFileNameToken()
        {
            var item = _CreateAudioItem(prefix: "42");
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "<file-name>"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(42u, item.Preview.AudioTagOverlay.Semantic().Track);
        }

        /// <summary>
        /// Verifies track template expansion composes with <see cref="AudioTagSetterOptions.TrackAutoIncrement"/>.
        /// </summary>
        [Fact]
        public void Track_TemplateSpan_AutoIncrement_AddsRenameListIndex()
        {
            var item = _CreateAudioItem(renameListIndex: 3, prefix: "10");
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(
                    Track: new AudioTagStringFieldOptions(Text: "<file-name>"),
                    TrackAutoIncrement: true
                )
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(13u, item.Preview.AudioTagOverlay.Semantic().Track);
        }

        /// <summary>
        /// Verifies non-numeric <c>track.text</c> after formatter expansion throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Track_TemplateSpan_NonNumeric_ThrowsFormatException()
        {
            var item = _CreateAudioItem(prefix: "Noise");
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "<file-name>"))
            );

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("0-255", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies track value 0 clears the overlay track.
        /// </summary>
        [Fact]
        public void Track_ZeroWithoutIncrement_Clears()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(track: 7)
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "0"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Track);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> does not replace an existing track.
        /// </summary>
        [Fact]
        public void Track_IfEmpty_KeepsExisting()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(track: 3)
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "9", OnlyIfEmpty: true))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(3u, item.Preview.AudioTagOverlay.Semantic().Track);
        }

        /// <summary>
        /// Verifies invalid track <c>text</c> throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Track_Text_NonInteger_Throws_FormatException()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "nope"))
            );

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
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Track: new AudioTagStringFieldOptions(Text: "256"))
            );

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("255", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies composer <c>text</c> may list several names separated by <c>;</c>.
        /// </summary>
        [Fact]
        public void Apply_Composers_SemicolonSeparated_SetsJoinedPreviewString()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Composers: new AudioTagStringFieldOptions(Text: "Bach ; Handel"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Bach; Handel", item.Preview.AudioTagOverlay.Semantic().Composers);
        }

        /// <summary>
        /// Verifies lyrics, grouping, and copyright set together.
        /// </summary>
        [Fact]
        public void Apply_Lyrics_Grouping_Copyright_SetsValues()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(
                    Lyrics: new AudioTagStringFieldOptions(Text: "La la"),
                    Grouping: new AudioTagStringFieldOptions(Text: "Suite"),
                    Copyright: new AudioTagStringFieldOptions(Text: "© 2004")
                )
            );

            filter.Setup();
            filter.Apply(item);

            var semantic = item.Preview.AudioTagOverlay.Semantic();
            Assert.Equal("La la", semantic.Lyrics);
            Assert.Equal("Suite", semantic.Grouping);
            Assert.Equal("© 2004", semantic.Copyright);
        }

        /// <summary>
        /// Verifies conductor and BPM set together.
        /// </summary>
        [Fact]
        public void Apply_Conductor_BeatsPerMinute_SetsValues()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(
                    Conductor: new AudioTagStringFieldOptions(Text: "Karajan"),
                    BeatsPerMinute: new AudioTagStringFieldOptions(Text: "120")
                )
            );

            filter.Setup();
            filter.Apply(item);

            var semantic = item.Preview.AudioTagOverlay.Semantic();
            Assert.Equal("Karajan", semantic.Conductor);
            Assert.Equal(120u, semantic.BeatsPerMinute);
        }

        /// <summary>
        /// Verifies BPM value 0 clears the overlay tempo.
        /// </summary>
        [Fact]
        public void BeatsPerMinute_Zero_Clears()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(beatsPerMinute: 128)
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(BeatsPerMinute: new AudioTagStringFieldOptions(Text: "0"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Semantic().BeatsPerMinute);
        }

        /// <summary>
        /// Verifies BPM above 65535 throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void BeatsPerMinute_Above65535_Throws_FormatException()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(BeatsPerMinute: new AudioTagStringFieldOptions(Text: "65536"))
            );

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("65535", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> leaves non-empty lyrics unchanged.
        /// </summary>
        [Fact]
        public void IfEmpty_Lyrics_LeavesNonEmpty()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(lyrics: "Kept")
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Lyrics: new AudioTagStringFieldOptions(Text: "Other", OnlyIfEmpty: true))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal("Kept", item.Preview.AudioTagOverlay.Semantic().Lyrics);
        }

        /// <summary>
        /// Verifies track count, disc, and disc count parse and set together.
        /// </summary>
        [Fact]
        public void Apply_TrackCount_Disc_DiscCount_SetsValues()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(
                    TrackCount: new AudioTagStringFieldOptions(Text: "12"),
                    Disc: new AudioTagStringFieldOptions(Text: "2"),
                    DiscCount: new AudioTagStringFieldOptions(Text: "3")
                )
            );

            filter.Setup();
            filter.Apply(item);

            var semantic = item.Preview.AudioTagOverlay.Semantic();
            Assert.Equal(12u, semantic.TrackCount);
            Assert.Equal(2u, semantic.Disc);
            Assert.Equal(3u, semantic.DiscCount);
        }

        /// <summary>
        /// Verifies disc value 0 clears the overlay disc.
        /// </summary>
        [Fact]
        public void Disc_Zero_Clears()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(disc: 2)
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Disc: new AudioTagStringFieldOptions(Text: "0"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Disc);
        }

        /// <summary>
        /// Verifies track count above 255 throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void TrackCount_Above255_Throws_FormatException()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(TrackCount: new AudioTagStringFieldOptions(Text: "256"))
            );

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("255", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies <see cref="AudioTagStringFieldOptions.OnlyIfEmpty"/> does not replace an existing disc.
        /// </summary>
        [Fact]
        public void Disc_IfEmpty_KeepsExisting()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(disc: 1)
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Disc: new AudioTagStringFieldOptions(Text: "9", OnlyIfEmpty: true))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(1u, item.Preview.AudioTagOverlay.Semantic().Disc);
        }

        /// <summary>
        /// Verifies year clears when value is 0.
        /// </summary>
        [Fact]
        public void Year_Zero_Clears()
        {
            var item = _CreateAudioItem(configureOriginal: m =>
                m.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(year: 1999)
            );
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "0"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Null(item.Preview.AudioTagOverlay.Semantic().Year);
        }

        /// <summary>
        /// Verifies year above 9999 fails preview with <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Year_Above9999_Throws_FormatException()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "12000"))
            );

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("9999", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies <c>year.text</c> with a formatter span expands and parses like a literal year integer.
        /// </summary>
        [Fact]
        public void Year_TemplateSpan_CompilesFileNameToken()
        {
            var item = _CreateAudioItem(prefix: "1999");
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "<file-name>"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(1999u, item.Preview.AudioTagOverlay.Semantic().Year);
        }

        /// <summary>
        /// Verifies non-numeric <c>year.text</c> after formatter expansion throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Year_TemplateSpan_NonNumeric_ThrowsFormatException()
        {
            var item = _CreateAudioItem(prefix: "Noise");
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "<file-name>"))
            );

            filter.Setup();
            var ex = Assert.Throws<FormatException>(() => filter.Apply(item));
            Assert.Contains("1-9999", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies year &gt; 9999 after formatter expansion throws <see cref="FormatException"/>.
        /// </summary>
        [Fact]
        public void Year_TemplateSpan_Above9999_ThrowsFormatException()
        {
            var item = _CreateAudioItem(prefix: "12000");
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "<file-name>"))
            );

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
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "2005"))
            );

            filter.Setup();
            filter.Apply(item);

            Assert.Equal(2005u, item.Preview.AudioTagOverlay.Semantic().Year);
        }

        /// <summary>
        /// Verifies invalid literal year <c>text</c> throws.
        /// </summary>
        [Fact]
        public void Year_TextLiteral_NonInteger_Throws()
        {
            var item = _CreateAudioItem();
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Year: new AudioTagStringFieldOptions(Text: "nope"))
            );

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
            item.Preview.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(title: "PreviewOnly");

            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "X"))
            );

            filter.Setup();
            var ex = Assert.Throws<InvalidOperationException>(() => filter.Apply(item));
            Assert.Contains("directory", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("PreviewOnly", item.Preview.AudioTagOverlay.Semantic().Title);
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
                  "type": "AudioTagSetter",
                  "options": {
                    "title": {
                      "text": "<file-name>"
                    },
                    "composers": {
                      "text": "Bach"
                    },
                    "lyrics": {
                      "text": "Verse"
                    },
                    "grouping": {
                      "text": "Work"
                    },
                    "copyright": {
                      "text": "© Label"
                    },
                    "conductor": {
                      "text": "Karajan"
                    },
                    "beatsPerMinute": {
                      "text": "128"
                    },
                    "year": {
                      "text": "2004",
                      "onlyIfEmpty": true
                    },
                    "track": {
                      "text": "1"
                    },
                    "trackCount": {
                      "text": "12"
                    },
                    "disc": {
                      "text": "2"
                    },
                    "discCount": {
                      "text": "3"
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
            var semantic = item.Preview.AudioTagOverlay.Semantic();
            Assert.Equal("P", semantic.Title);
            Assert.Equal("Bach", semantic.Composers);
            Assert.Equal("Verse", semantic.Lyrics);
            Assert.Equal("Work", semantic.Grouping);
            Assert.Equal("© Label", semantic.Copyright);
            Assert.Equal("Karajan", semantic.Conductor);
            Assert.Equal(128u, semantic.BeatsPerMinute);
            Assert.Equal(2004u, semantic.Year);
            Assert.Equal(3u, semantic.Track);
            Assert.Equal(12u, semantic.TrackCount);
            Assert.Equal(2u, semantic.Disc);
            Assert.Equal(3u, semantic.DiscCount);
        }
    }
}
