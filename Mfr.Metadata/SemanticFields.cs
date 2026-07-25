using System.Globalization;
using Mfr.Models.Tags;

namespace Mfr.Metadata
{
    /// <summary>
    /// Reads and writes <see cref="SemanticAudioField"/> values as filter/preview strings through the block-derived
    /// <see cref="SemanticAudioTag"/> layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Embeds use <see cref="SemanticAudioTag.FromOverlay"/> for reads; writes merge an updated <see cref="SemanticAudioTag"/> back into
    /// blocks via <see cref="AudioTagPersistence.MergeSemanticIntoBlocks"/> (broadcast to present blocks; recommended create when empty).
    /// </para>
    /// <para>
    /// Empty strings represent absent fields (same convention as <see cref="Models.AudioFieldTarget"/> previews).
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
        public static void SetSemanticField(
            AudioTagOverlay overlay,
            SemanticAudioField field,
            string fieldString)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var semantic = SemanticAudioTag.FromOverlay(overlay);
            var trimmed = fieldString.Trim();

            semantic = field switch
            {
                SemanticAudioField.Title => semantic with { Title = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Album => semantic with { Album = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Performers => semantic with { Performers = _NullIfEmptyString(trimmed) },
                SemanticAudioField.AlbumArtists => semantic with { AlbumArtists = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Composers => semantic with { Composers = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Genre => semantic with { Genre = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Comment => semantic with { Comment = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Lyrics => semantic with { Lyrics = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Copyright => semantic with { Copyright = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Grouping => semantic with { Grouping = _NullIfEmptyString(trimmed) },
                SemanticAudioField.Year => semantic with { Year = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                SemanticAudioField.Track => semantic with { Track = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                SemanticAudioField.TrackCount => semantic with { TrackCount = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                SemanticAudioField.Disc => semantic with { Disc = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                SemanticAudioField.DiscCount => semantic with { DiscCount = _ParseNullableUInt(trimmed, nameof(fieldString)) },
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
            };

            AudioTagPersistence.MergeSemanticIntoBlocks(overlay, semantic);
        }

        private static string _DecimalDigitsOrEmpty(uint? value)
        {
            return value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);
        }

        private static string? _NullIfEmptyString(string trimmed)
        {
            return trimmed.Length == 0 ? null : trimmed;
        }

        private static uint? _ParseNullableUInt(string trimmed, string valueParamName)
        {
            if (trimmed.Length == 0)
                return null;

            if (!uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new ArgumentException(
                    $"Value must be empty or a non-negative integer, got '{trimmed}'.",
                    valueParamName);
            }

            return parsed;
        }
    }
}
