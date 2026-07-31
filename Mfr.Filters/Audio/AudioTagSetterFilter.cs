using System.Globalization;
using System.Text.Json.Serialization;
using Mfr.Filters.Formatting;
using Mfr.Models;
using Mfr.Models.Tags;
using Mfr.Utils;

namespace Mfr.Filters.Audio
{
    /// <summary>
    /// Options for one string-valued audio overlay field (performers, title, comment, etc.), including
    /// <c>year</c>, <c>track</c>, <c>trackCount</c>, <c>disc</c>, and <c>discCount</c> in
    /// <see cref="AudioTagSetterOptions"/> where <c>text</c> is parsed as a number after formatting.
    /// </summary>
    /// <param name="Text">
    /// Plain text, or a formatter template when it contains at least one balanced <c>&lt;...&gt;</c> span
    /// that looks like a formatter token (same rules as the <see cref="FormatterFilter"/> template language).
    /// </param>
    /// <param name="OnlyIfEmpty">
    /// When <c>true</c>, set the tag only when the current preview overlay value is empty; when <c>false</c>, always set.
    /// </param>
    public sealed record AudioTagStringFieldOptions(
        [property: JsonPropertyName("text")] string Text = "",
        [property: JsonPropertyName("onlyIfEmpty")] bool OnlyIfEmpty = false);

    /// <summary>
    /// Batch options for <see cref="AudioTagSetterFilter"/> (legacy Audio / ID3 Tag Setter style).
    /// </summary>
    /// <param name="Performers">Primary performers; omit (or <c>null</c>) to leave unchanged.</param>
    /// <param name="AlbumArtists">Album artists; omit (or <c>null</c>) to leave unchanged.</param>
    /// <param name="Title">Track title options.</param>
    /// <param name="Album">Album name options.</param>
    /// <param name="Genre">Genre options (use <c>; </c> in text for multiple values).</param>
    /// <param name="Comment">Comment options.</param>
    /// <param name="Composers">Composers; omit (or <c>null</c>) to leave unchanged (use <c>; </c> in text for multiple values).</param>
    /// <param name="Lyrics">Unsynchronised lyrics; omit (or <c>null</c>) to leave unchanged.</param>
    /// <param name="Grouping">Content group / work title; omit (or <c>null</c>) to leave unchanged.</param>
    /// <param name="Copyright">Copyright notice; omit (or <c>null</c>) to leave unchanged.</param>
    /// <param name="Year">
    /// Release year; same shape as other string fields. After formatting (or literal <c>text</c>), the result must be
    /// empty (clear year), <c>0</c> (clear), or an integer <c>1</c>-<c>9999</c>. Anything else yields a preview error.
    /// </param>
    /// <param name="Track">
    /// Track index; same <c>text</c> / <c>onlyIfEmpty</c> shape as <paramref name="Year"/>. After formatting, empty clears;
    /// otherwise the value must parse as an integer <c>0</c>-<c>255</c> (base before increment). Non-numeric or out of range yields a preview error. With <paramref name="TrackAutoIncrement"/>, <see cref="FileMeta.RenameListIndex"/> is added and the sum is clamped to 255.
    /// </param>
    /// <param name="TrackCount">
    /// Track count (of n/m); same parse rules as <paramref name="Track"/> without auto-increment (empty or <c>0</c> clears; <c>1</c>-<c>255</c> sets).
    /// </param>
    /// <param name="Disc">
    /// Disc index; same parse rules as <paramref name="TrackCount"/>.
    /// </param>
    /// <param name="DiscCount">
    /// Disc count; same parse rules as <paramref name="TrackCount"/>.
    /// </param>
    /// <param name="TrackAutoIncrement">
    /// When true and <paramref name="Track"/> is active, add each item’s <see cref="FileMeta.RenameListIndex"/> to the parsed base track before clamping to 255 (legacy “auto-increment track” checkbox).
    /// </param>
    public sealed record AudioTagSetterOptions(
        [property: JsonPropertyName("performers")] AudioTagStringFieldOptions? Performers = null,
        [property: JsonPropertyName("albumArtists")] AudioTagStringFieldOptions? AlbumArtists = null,
        [property: JsonPropertyName("title")] AudioTagStringFieldOptions? Title = null,
        [property: JsonPropertyName("album")] AudioTagStringFieldOptions? Album = null,
        [property: JsonPropertyName("genre")] AudioTagStringFieldOptions? Genre = null,
        [property: JsonPropertyName("comment")] AudioTagStringFieldOptions? Comment = null,
        [property: JsonPropertyName("composers")] AudioTagStringFieldOptions? Composers = null,
        [property: JsonPropertyName("lyrics")] AudioTagStringFieldOptions? Lyrics = null,
        [property: JsonPropertyName("grouping")] AudioTagStringFieldOptions? Grouping = null,
        [property: JsonPropertyName("copyright")] AudioTagStringFieldOptions? Copyright = null,
        [property: JsonPropertyName("year")] AudioTagStringFieldOptions? Year = null,
        [property: JsonPropertyName("track")] AudioTagStringFieldOptions? Track = null,
        [property: JsonPropertyName("trackCount")] AudioTagStringFieldOptions? TrackCount = null,
        [property: JsonPropertyName("disc")] AudioTagStringFieldOptions? Disc = null,
        [property: JsonPropertyName("discCount")] AudioTagStringFieldOptions? DiscCount = null,
        [property: JsonPropertyName("trackAutoIncrement")] bool TrackAutoIncrement = false);

    /// <summary>
    /// Sets common embedded audio-tag fields on each file row (multi-format via the shared overlay model).
    /// </summary>
    /// <remarks>
    /// <para>
    /// For files, the filter calls <see cref="RenameItemEmbeddedTagsExtensions.EnsureEmbeddedTagsLoaded"/> so preview tags reflect disk before applying per-field options.
    /// Writes broadcast onto every present tag block; an empty overlay gets the container's recommended empty block first.
    /// Directory rows cannot load tags and surface the same <see cref="InvalidOperationException"/> as other audio overlay operations
    /// (caught during preview and shown as the row’s <see cref="RenameItem.PreviewError"/>).
    /// </para>
    /// </remarks>
    /// <param name="Options">Per-field behaviors and values.</param>
    public sealed record AudioTagSetterFilter(
        AudioTagSetterOptions Options) : BaseFilter
    {
        /// <summary>
        /// Formatter used when a field is omitted from options: always expands to empty string (field is not applied).
        /// </summary>
        private Formatter PerformersFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter AlbumArtistsFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter TitleFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter AlbumFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter GenreFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter CommentFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter ComposersFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter LyricsFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter GroupingFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter CopyrightFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter YearFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter TrackFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter TrackCountFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter DiscFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter DiscCountFormatter = FormatStringCompiler.EmptyFormatter;

        /// <inheritdoc />
        public override string Type => "AudioTagSetter";

        /// <inheritdoc />
        protected override void _Setup()
        {
            PerformersFormatter = _CreateFormatter(Options.Performers);
            AlbumArtistsFormatter = _CreateFormatter(Options.AlbumArtists);
            TitleFormatter = _CreateFormatter(Options.Title);
            AlbumFormatter = _CreateFormatter(Options.Album);
            GenreFormatter = _CreateFormatter(Options.Genre);
            CommentFormatter = _CreateFormatter(Options.Comment);
            ComposersFormatter = _CreateFormatter(Options.Composers);
            LyricsFormatter = _CreateFormatter(Options.Lyrics);
            GroupingFormatter = _CreateFormatter(Options.Grouping);
            CopyrightFormatter = _CreateFormatter(Options.Copyright);
            YearFormatter = _CreateFormatter(Options.Year);
            TrackFormatter = _CreateFormatter(Options.Track);
            TrackCountFormatter = _CreateFormatter(Options.TrackCount);
            DiscFormatter = _CreateFormatter(Options.Disc);
            DiscCountFormatter = _CreateFormatter(Options.DiscCount);
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.EnsureEmbeddedTagsLoaded();
            var tags = item.Preview.AudioTagOverlay;
            var semanticTag = SemanticAudioTag.FromOverlay(tags);

            if (Options.Performers is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Performers.OnlyIfEmpty,
                    semanticTag.Performers,
                    PerformersFormatter,
                    static (m, v) => m with { Performers = v });

            if (Options.AlbumArtists is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.AlbumArtists.OnlyIfEmpty,
                    semanticTag.AlbumArtists,
                    AlbumArtistsFormatter,
                    static (m, v) => m with { AlbumArtists = v });

            if (Options.Title is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Title.OnlyIfEmpty,
                    semanticTag.Title,
                    TitleFormatter,
                    static (m, v) => m with { Title = v });

            if (Options.Album is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Album.OnlyIfEmpty,
                    semanticTag.Album,
                    AlbumFormatter,
                    static (m, v) => m with { Album = v });

            if (Options.Genre is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Genre.OnlyIfEmpty,
                    semanticTag.Genre,
                    GenreFormatter,
                    static (m, v) => m with { Genre = v });

            if (Options.Comment is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Comment.OnlyIfEmpty,
                    semanticTag.Comment,
                    CommentFormatter,
                    static (m, v) => m with { Comment = v });

            if (Options.Composers is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Composers.OnlyIfEmpty,
                    semanticTag.Composers,
                    ComposersFormatter,
                    static (m, v) => m with { Composers = v });

            if (Options.Lyrics is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Lyrics.OnlyIfEmpty,
                    semanticTag.Lyrics,
                    LyricsFormatter,
                    static (m, v) => m with { Lyrics = v });

            if (Options.Grouping is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Grouping.OnlyIfEmpty,
                    semanticTag.Grouping,
                    GroupingFormatter,
                    static (m, v) => m with { Grouping = v });

            if (Options.Copyright is not null)
                semanticTag = _ApplyStringField(
                    item,
                    semanticTag,
                    Options.Copyright.OnlyIfEmpty,
                    semanticTag.Copyright,
                    CopyrightFormatter,
                    static (m, v) => m with { Copyright = v });

            if (Options.Year is not null)
                semanticTag = _ApplyYearField(item, semanticTag, Options.Year.OnlyIfEmpty, YearFormatter);

            if (Options.Track is not null)
            {
                var trackIncrement = Options.TrackAutoIncrement ? item.Original.RenameListIndex : 0;
                semanticTag = _ApplyByteField(
                    item,
                    semanticTag,
                    Options.Track.OnlyIfEmpty,
                    semanticTag.Track,
                    TrackFormatter,
                    fieldLabel: "track",
                    static (m, v) => m with { Track = v },
                    autoIncrementBy: trackIncrement);
            }

            if (Options.TrackCount is not null)
            {
                semanticTag = _ApplyByteField(
                    item,
                    semanticTag,
                    Options.TrackCount.OnlyIfEmpty,
                    semanticTag.TrackCount,
                    TrackCountFormatter,
                    fieldLabel: "trackCount",
                    static (m, v) => m with { TrackCount = v });
            }

            if (Options.Disc is not null)
            {
                semanticTag = _ApplyByteField(
                    item,
                    semanticTag,
                    Options.Disc.OnlyIfEmpty,
                    semanticTag.Disc,
                    DiscFormatter,
                    fieldLabel: "disc",
                    static (m, v) => m with { Disc = v });
            }

            if (Options.DiscCount is not null)
            {
                semanticTag = _ApplyByteField(
                    item,
                    semanticTag,
                    Options.DiscCount.OnlyIfEmpty,
                    semanticTag.DiscCount,
                    DiscCountFormatter,
                    fieldLabel: "discCount",
                    static (m, v) => m with { DiscCount = v });
            }

            if (!_HasAnyConfiguredSemanticField())
                return;

            tags.MergeSemantic(semanticTag);
        }

        /// <summary>
        /// Returns whether <see cref="Options"/> includes at least one field specification (not omitted from JSON).
        /// </summary>
        private bool _HasAnyConfiguredSemanticField()
        {
            return Options.Performers is not null
                || Options.AlbumArtists is not null
                || Options.Title is not null
                || Options.Album is not null
                || Options.Genre is not null
                || Options.Comment is not null
                || Options.Composers is not null
                || Options.Lyrics is not null
                || Options.Grouping is not null
                || Options.Copyright is not null
                || Options.Year is not null
                || Options.Track is not null
                || Options.TrackCount is not null
                || Options.Disc is not null
                || Options.DiscCount is not null;
        }

        /// <summary>
        /// Creates a per-item formatter for <paramref name="spec"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <paramref name="spec"/> is <see langword="null"/> (field omitted from options), returns
        /// <see cref="FormatStringCompiler.EmptyFormatter"/>.
        /// </para>
        /// <para>
        /// Otherwise compiles formatter templates when <see cref="FormatStringCompiler.ContainsLikelyFormatTokens"/> is true,
        /// or returns a delegate that yields the literal <c>text</c>.
        /// </para>
        /// </remarks>
        private static Formatter _CreateFormatter(AudioTagStringFieldOptions? spec)
        {
            if (spec is null)
                return FormatStringCompiler.EmptyFormatter;

            if (FormatStringCompiler.ContainsLikelyFormatTokens(spec.Text))
                return FormatStringCompiler.Compile(spec.Text);

            var literal = spec.Text;
            return _ => literal;
        }

        private static SemanticAudioTag _ApplyStringField(
            RenameItem item,
            SemanticAudioTag semantic,
            bool onlyIfEmpty,
            string? currentValue,
            Formatter formatter,
            Func<SemanticAudioTag, string?, SemanticAudioTag> assignUpdated)
        {
            var overlayAlreadyHasValue = !string.IsNullOrWhiteSpace(currentValue);
            if (onlyIfEmpty && overlayAlreadyHasValue)
                return semantic;

            var expanded = formatter(item).TrimmedOrNull();
            return assignUpdated(semantic, expanded);
        }

        private SemanticAudioTag _ApplyYearField(
            RenameItem item,
            SemanticAudioTag semantic,
            bool onlyIfEmpty,
            Formatter formatter)
        {
            if (onlyIfEmpty && semantic.Year is not null)
                return semantic;

            var resolved = formatter(item);
            var trimmed = resolved.Trim();
            if (trimmed.Length == 0)
                return semantic with { Year = null };

            if (!uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yearValue))
            {
                throw new FormatException(
                    $"AudioTagSetter year must be empty, 0, or an integer 1-9999 after formatting. Got '{trimmed}'.");
            }

            if (yearValue > 9999u)
            {
                throw new FormatException(
                    $"AudioTagSetter year must be between 0 and 9999. Got {yearValue}.");
            }

            if (yearValue == 0)
                return semantic with { Year = null };

            return semantic with { Year = yearValue };
        }

        /// <summary>
        /// Parses a 0–255 overlay integer field (track, track count, disc, disc count) after formatting.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <paramref name="autoIncrementBy"/> is non-zero (track auto-increment), it is added to the parsed
        /// base and the sum is clamped to 255; a non-positive sum clears the field.
        /// </para>
        /// </remarks>
        private static SemanticAudioTag _ApplyByteField(
            RenameItem item,
            SemanticAudioTag semantic,
            bool onlyIfEmpty,
            uint? currentValue,
            Formatter formatter,
            string fieldLabel,
            Func<SemanticAudioTag, uint?, SemanticAudioTag> assignUpdated,
            int autoIncrementBy = 0)
        {
            if (onlyIfEmpty && currentValue is not null)
                return semantic;

            var resolved = formatter(item);
            var trimmed = resolved.Trim();
            if (trimmed.Length == 0)
                return assignUpdated(semantic, null);

            if (!uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new FormatException(
                    $"AudioTagSetter {fieldLabel} must be empty, or an integer 0-255 after formatting. Got '{trimmed}'.");
            }

            if (parsed > 255u)
            {
                throw new FormatException(
                    $"AudioTagSetter {fieldLabel} must be between 0 and 255. Got {parsed}.");
            }

            long raw = parsed + autoIncrementBy;
            if (raw <= 0)
                return assignUpdated(semantic, null);

            return assignUpdated(semantic, (uint)Math.Min(raw, 255));
        }
    }
}
