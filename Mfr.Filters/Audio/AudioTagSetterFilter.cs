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
    /// <c>year</c> and <c>track</c> in <see cref="AudioTagSetterOptions"/> where <c>text</c> is parsed as a number after formatting.
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
    /// <param name="Year">
    /// Release year; same shape as other string fields. After formatting (or literal <c>text</c>), the result must be
    /// empty (clear year), <c>0</c> (clear), or an integer <c>1</c>-<c>9999</c>. Anything else yields a preview error.
    /// </param>
    /// <param name="Track">
    /// Track index; same <c>text</c> / <c>onlyIfEmpty</c> shape as <paramref name="Year"/>. After formatting, empty clears;
    /// otherwise the value must parse as an integer <c>0</c>-<c>255</c> (base before increment). Non-numeric or out of range yields a preview error. With <paramref name="TrackAutoIncrement"/>, <see cref="FileMeta.RenameListIndex"/> is added and the sum is clamped to 255.
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
        [property: JsonPropertyName("year")] AudioTagStringFieldOptions? Year = null,
        [property: JsonPropertyName("track")] AudioTagStringFieldOptions? Track = null,
        [property: JsonPropertyName("trackAutoIncrement")] bool TrackAutoIncrement = false);

    /// <summary>
    /// Sets common embedded audio-tag fields on each file row (multi-format via the shared overlay model).
    /// </summary>
    /// <remarks>
    /// <para>
    /// For files, the filter calls <see cref="RenameItem.EnsureAudioTagsLoaded"/> so preview tags reflect disk before applying per-field options.
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
        private Formatter YearFormatter = FormatStringCompiler.EmptyFormatter;
        private Formatter TrackFormatter = FormatStringCompiler.EmptyFormatter;

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
            YearFormatter = _CreateFormatter(Options.Year);
            TrackFormatter = _CreateFormatter(Options.Track);
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            item.EnsureAudioTagsLoaded();
            var tags = item.Preview.AudioTagOverlay;

            if (Options.Performers is not null)
                _ApplyStringField(
                    item,
                    Options.Performers.OnlyIfEmpty,
                    tags.Performers,
                    PerformersFormatter,
                    v => tags.Performers = v);
            if (Options.AlbumArtists is not null)
                _ApplyStringField(
                    item,
                    Options.AlbumArtists.OnlyIfEmpty,
                    tags.AlbumArtists,
                    AlbumArtistsFormatter,
                    v => tags.AlbumArtists = v);
            if (Options.Title is not null)
                _ApplyStringField(
                    item,
                    Options.Title.OnlyIfEmpty,
                    tags.Title,
                    TitleFormatter,
                    v => tags.Title = v);
            if (Options.Album is not null)
                _ApplyStringField(
                    item,
                    Options.Album.OnlyIfEmpty,
                    tags.Album,
                    AlbumFormatter,
                    v => tags.Album = v);
            if (Options.Genre is not null)
                _ApplyStringField(
                    item,
                    Options.Genre.OnlyIfEmpty,
                    tags.Genre,
                    GenreFormatter,
                    v => tags.Genre = v);
            if (Options.Comment is not null)
                _ApplyStringField(
                    item,
                    Options.Comment.OnlyIfEmpty,
                    tags.Comment,
                    CommentFormatter,
                    v => tags.Comment = v);

            if (Options.Year is not null)
                _ApplyYear(
                    item,
                    Options.Year.OnlyIfEmpty,
                    YearFormatter,
                    tags);
            if (Options.Track is not null)
                _ApplyTrackField(
                    item,
                    Options.Track.OnlyIfEmpty,
                    TrackFormatter,
                    tags);

            if (_HasAnyConfiguredSemanticField())
                RenameItemPreviewAudioSemantics.TryFlushPreviewAudioFacadeIntoNativeBlocks(item);
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
                || Options.Year is not null
                || Options.Track is not null;
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

        private static void _ApplyStringField(
            RenameItem item,
            bool onlyIfEmpty,
            string? currentValue,
            Formatter formatter,
            Action<string?> assign)
        {
            var overlayAlreadyHasValue = !string.IsNullOrWhiteSpace(currentValue);
            if (onlyIfEmpty && overlayAlreadyHasValue)
                return;

            var expanded = formatter(item);
            assign(expanded.TrimmedOrNull());
        }

        private void _ApplyYear(
            RenameItem item,
            bool onlyIfEmpty,
            Formatter formatter,
            AudioTagOverlay tags)
        {
            if (onlyIfEmpty && tags.Year is not null)
                return;

            var resolved = formatter(item);
            var trimmed = resolved.Trim();
            if (trimmed.Length == 0)
            {
                tags.Year = null;
                return;
            }

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
            {
                tags.Year = null;
                return;
            }

            tags.Year = yearValue;
        }

        private void _ApplyTrackField(
            RenameItem item,
            bool onlyIfEmpty,
            Formatter formatter,
            AudioTagOverlay tags)
        {
            if (onlyIfEmpty && tags.Track is not null)
                return;

            var resolved = formatter(item);
            var trimmed = resolved.Trim();
            if (trimmed.Length == 0)
            {
                tags.Track = null;
                return;
            }

            if (!uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseTrack))
            {
                throw new FormatException(
                    $"AudioTagSetter track must be empty, or an integer 0-255 after formatting. Got '{trimmed}'.");
            }

            if (baseTrack > 255u)
            {
                throw new FormatException(
                    $"AudioTagSetter track base value must be between 0 and 255. Got {baseTrack}.");
            }

            long raw = baseTrack;
            if (Options.TrackAutoIncrement)
                raw += item.Original.RenameListIndex;

            if (raw <= 0)
            {
                tags.Track = null;
                return;
            }

            tags.Track = (uint)Math.Min(raw, 255);
        }
    }
}
