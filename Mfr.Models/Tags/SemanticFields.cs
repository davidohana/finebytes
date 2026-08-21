using System.Globalization;
using Mfr.Utils;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Reads and writes <see cref="SemanticAudioField"/> values as filter/preview strings through the block-derived
    /// <see cref="SemanticAudioTag"/> layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Embeds use <see cref="SemanticAudioTag.FromOverlay"/> for reads; writes merge an updated <see cref="SemanticAudioTag"/> back into
    /// blocks via <see cref="AudioTagOverlay.MergeSemantic"/> (broadcast to present blocks; recommended create when empty).
    /// </para>
    /// <para>
    /// Empty strings represent absent fields (same convention as <see cref="Filters.SemanticAudioFieldTarget"/> previews).
    /// </para>
    /// </remarks>
    public static class SemanticFields
    {
        /// <summary>
        /// Formats <paramref name="field"/> using <paramref name="semantic"/> snapshot values (empty strings replace absent semantics).
        /// </summary>
        /// <param name="semantic">Projected semantics; typically from <see cref="SemanticAudioTag.FromOverlay"/>.</param>
        /// <param name="field">Which semantic field to format.</param>
        /// <returns>Filter/preview string for the field (empty when unset).</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="field"/> is unrecognized.</exception>
        public static string Format(SemanticAudioTag semantic, SemanticAudioField field)
        {
            return field switch
            {
                SemanticAudioField.Title => semantic.Title ?? string.Empty,
                SemanticAudioField.Album => semantic.Album ?? string.Empty,
                SemanticAudioField.Performers => semantic.Performers ?? string.Empty,
                SemanticAudioField.AlbumArtists => semantic.AlbumArtists ?? string.Empty,
                SemanticAudioField.Composers => semantic.Composers ?? string.Empty,
                SemanticAudioField.Genre => semantic.Genre ?? string.Empty,
                SemanticAudioField.Comment => semantic.Comment ?? string.Empty,
                SemanticAudioField.Lyrics => semantic.Lyrics ?? string.Empty,
                SemanticAudioField.Copyright => semantic.Copyright ?? string.Empty,
                SemanticAudioField.Grouping => semantic.Grouping ?? string.Empty,
                SemanticAudioField.Year => _DecimalDigitsOrEmpty(semantic.Year),
                SemanticAudioField.Track => _DecimalDigitsOrEmpty(semantic.Track),
                SemanticAudioField.TrackCount => _DecimalDigitsOrEmpty(semantic.TrackCount),
                SemanticAudioField.Disc => _DecimalDigitsOrEmpty(semantic.Disc),
                SemanticAudioField.DiscCount => _DecimalDigitsOrEmpty(semantic.DiscCount),
                SemanticAudioField.BeatsPerMinute => _DecimalDigitsOrEmpty(semantic.BeatsPerMinute),
                SemanticAudioField.Conductor => semantic.Conductor ?? string.Empty,
                SemanticAudioField.MusicBrainzArtistId => semantic.MusicBrainzArtistId ?? string.Empty,
                SemanticAudioField.MusicBrainzReleaseId => semantic.MusicBrainzReleaseId ?? string.Empty,
                SemanticAudioField.MusicBrainzReleaseArtistId => semantic.MusicBrainzReleaseArtistId ?? string.Empty,
                SemanticAudioField.MusicBrainzTrackId => semantic.MusicBrainzTrackId ?? string.Empty,
                SemanticAudioField.MusicBrainzDiscId => semantic.MusicBrainzDiscId ?? string.Empty,
                SemanticAudioField.MusicBrainzReleaseStatus => semantic.MusicBrainzReleaseStatus ?? string.Empty,
                SemanticAudioField.MusicBrainzReleaseType => semantic.MusicBrainzReleaseType ?? string.Empty,
                SemanticAudioField.MusicBrainzReleaseCountry => semantic.MusicBrainzReleaseCountry ?? string.Empty,
                SemanticAudioField.MusicIpId => semantic.MusicIpId ?? string.Empty,
                SemanticAudioField.AmazonId => semantic.AmazonId ?? string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
            };
        }

        /// <summary>
        /// Returns the filter/preview string for <paramref name="field"/> from the block projection of <paramref name="overlay"/>.
        /// </summary>
        /// <param name="overlay">Structured tag blocks.</param>
        /// <param name="field">Logical semantic audio field.</param>
        /// <returns>Same formatting as <see cref="Format"/> (empty when unset).</returns>
        public static string GetSemanticField(AudioTagOverlay overlay, SemanticAudioField field)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var semantic = SemanticAudioTag.FromOverlay(overlay);
            return Format(semantic, field);
        }

        /// <summary>
        /// Parses <paramref name="fieldString"/> for <paramref name="field"/>, merges the updated <see cref="SemanticAudioTag"/> into
        /// <paramref name="overlay"/> blocks (broadcast / recommended create).
        /// </summary>
        /// <param name="overlay">Overlay whose blocks are updated in place.</param>
        /// <param name="field">Which semantic field to replace.</param>
        /// <param name="fieldString">Text as-is, or decimal digits for numeric fields; empty clears nullable fields.</param>
        /// <exception cref="ArgumentException">Thrown when a numeric field string is not empty and not a valid non-negative integer.</exception>
        public static void SetSemanticField(AudioTagOverlay overlay, SemanticAudioField field, string fieldString)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var semantic = SemanticAudioTag.FromOverlay(overlay);
            var trimmed = fieldString.Trim();

            semantic = field switch
            {
                SemanticAudioField.Title => semantic with { Title = trimmed.TrimmedOrNull() },
                SemanticAudioField.Album => semantic with { Album = trimmed.TrimmedOrNull() },
                SemanticAudioField.Performers => semantic with { Performers = trimmed.TrimmedOrNull() },
                SemanticAudioField.AlbumArtists => semantic with { AlbumArtists = trimmed.TrimmedOrNull() },
                SemanticAudioField.Composers => semantic with { Composers = trimmed.TrimmedOrNull() },
                SemanticAudioField.Genre => semantic with { Genre = trimmed.TrimmedOrNull() },
                SemanticAudioField.Comment => semantic with { Comment = trimmed.TrimmedOrNull() },
                SemanticAudioField.Lyrics => semantic with { Lyrics = trimmed.TrimmedOrNull() },
                SemanticAudioField.Copyright => semantic with { Copyright = trimmed.TrimmedOrNull() },
                SemanticAudioField.Grouping => semantic with { Grouping = trimmed.TrimmedOrNull() },
                SemanticAudioField.Year => semantic with { Year = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                SemanticAudioField.Track => semantic with { Track = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                SemanticAudioField.TrackCount => semantic with
                {
                    TrackCount = _ParseNullableUInt(trimmed, nameof(fieldString)),
                },
                SemanticAudioField.Disc => semantic with { Disc = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                SemanticAudioField.DiscCount => semantic with
                {
                    DiscCount = _ParseNullableUInt(trimmed, nameof(fieldString)),
                },
                SemanticAudioField.BeatsPerMinute => semantic with
                {
                    BeatsPerMinute = _ParseNullableUInt(trimmed, nameof(fieldString)),
                },
                SemanticAudioField.Conductor => semantic with { Conductor = trimmed.TrimmedOrNull() },
                SemanticAudioField.MusicBrainzArtistId => semantic with
                {
                    MusicBrainzArtistId = trimmed.TrimmedOrNull(),
                },
                SemanticAudioField.MusicBrainzReleaseId => semantic with
                {
                    MusicBrainzReleaseId = trimmed.TrimmedOrNull(),
                },
                SemanticAudioField.MusicBrainzReleaseArtistId => semantic with
                {
                    MusicBrainzReleaseArtistId = trimmed.TrimmedOrNull(),
                },
                SemanticAudioField.MusicBrainzTrackId => semantic with { MusicBrainzTrackId = trimmed.TrimmedOrNull() },
                SemanticAudioField.MusicBrainzDiscId => semantic with { MusicBrainzDiscId = trimmed.TrimmedOrNull() },
                SemanticAudioField.MusicBrainzReleaseStatus => semantic with
                {
                    MusicBrainzReleaseStatus = trimmed.TrimmedOrNull(),
                },
                SemanticAudioField.MusicBrainzReleaseType => semantic with
                {
                    MusicBrainzReleaseType = trimmed.TrimmedOrNull(),
                },
                SemanticAudioField.MusicBrainzReleaseCountry => semantic with
                {
                    MusicBrainzReleaseCountry = trimmed.TrimmedOrNull(),
                },
                SemanticAudioField.MusicIpId => semantic with { MusicIpId = trimmed.TrimmedOrNull() },
                SemanticAudioField.AmazonId => semantic with { AmazonId = trimmed.TrimmedOrNull() },
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
            };

            overlay.MergeSemantic(semantic);
        }

        private static string _DecimalDigitsOrEmpty(uint? value)
        {
            return value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);
        }

        private static uint? _ParseNullableUInt(string trimmed, string valueParamName)
        {
            if (trimmed.Length == 0)
                return null;

            if (!uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new ArgumentException(
                    $"Value must be empty or a non-negative integer, got '{trimmed}'.",
                    valueParamName
                );
            }

            return parsed;
        }
    }
}
