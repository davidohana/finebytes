using System.Globalization;
using Mfr.Models.Tags;

namespace Mfr.Metadata
{
    /// <summary>
    /// Reads and writes <see cref="AudioOverlayField"/> values through the block-derived <see cref="AudioTagSemanticSurface"/> layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Embeds use <see cref="AudioTagSemanticSurface.FromBlocks"/> for reads; writes merge an updated surface back into native
    /// blocks via <see cref="AudioTagPersistence.MergeSemanticOntoNativeBlocks"/>.
    /// </para>
    /// </remarks>
    public static class AudioOverlaySemanticIo
    {
        /// <summary>
        /// Returns the invariant string for <paramref name="field"/> from the block projection of <paramref name="overlay"/>.
        /// </summary>
        /// <param name="overlay">Structured tag blocks.</param>
        /// <param name="field">Logical audio field.</param>
        /// <returns>Same formatting as <see cref="AudioOverlaySemanticFieldStrings.Format"/>.</returns>
        public static string GetFieldString(AudioTagOverlay overlay, AudioOverlayField field)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            return AudioOverlaySemanticFieldStrings.Format(AudioTagSemanticSurface.FromBlocks(overlay), field);
        }

        /// <summary>
        /// Parses <paramref name="invariantString"/> for <paramref name="field"/>, merges the updated semantic surface into
        /// <paramref name="overlay"/> blocks, and optionally uses <paramref name="embeddedTagSourcePath"/> for Apple atom coalescence.
        /// </summary>
        /// <param name="overlay">Overlay whose blocks are updated in place.</param>
        /// <param name="field">Which semantic field to replace.</param>
        /// <param name="invariantString">Trimmed or raw value (numeric fields use invariant integer rules).</param>
        /// <param name="embeddedTagSourcePath">On-disk file path for TagLib, or <see langword="null"/> to skip live-file Apple merge.</param>
        /// <exception cref="ArgumentException">Thrown when a numeric field string is not empty and not a valid non-negative integer.</exception>
        public static void MergeInvariantStringIntoOverlay(
            AudioTagOverlay overlay,
            AudioOverlayField field,
            string invariantString,
            string? embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var merged = AudioTagSemanticSurface.FromBlocks(overlay);
            var trimmed = invariantString.Trim();

            merged = field switch
            {
                AudioOverlayField.Title => merged with { Title = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Album => merged with { Album = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Performers => merged with { Performers = _NullIfEmptyString(trimmed) },
                AudioOverlayField.AlbumArtists => merged with { AlbumArtists = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Composers => merged with { Composers = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Genre => merged with { Genre = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Comment => merged with { Comment = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Lyrics => merged with { Lyrics = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Copyright => merged with { Copyright = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Grouping => merged with { Grouping = _NullIfEmptyString(trimmed) },
                AudioOverlayField.Year => merged with { Year = _ParseNullableUInt(trimmed, nameof(invariantString)) },
                AudioOverlayField.Track => merged with { Track = _ParseNullableUInt(trimmed, nameof(invariantString)) },
                AudioOverlayField.TrackCount => merged with { TrackCount = _ParseNullableUInt(trimmed, nameof(invariantString)) },
                AudioOverlayField.Disc => merged with { Disc = _ParseNullableUInt(trimmed, nameof(invariantString)) },
                AudioOverlayField.DiscCount => merged with { DiscCount = _ParseNullableUInt(trimmed, nameof(invariantString)) },
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
            };

            AudioTagPersistence.MergeSemanticOntoNativeBlocks(overlay, merged, embeddedTagSourcePath);
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
                    $"Value must be empty or a non-negative integer (invariant), got '{trimmed}'.",
                    valueParamName);
            }

            return parsed;
        }
    }
}
