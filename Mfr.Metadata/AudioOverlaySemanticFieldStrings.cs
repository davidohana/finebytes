using System.Globalization;
using Mfr.Models.Tags;

namespace Mfr.Metadata
{
    /// <summary>
    /// Invariant embedded-tag strings from <see cref="AudioTagSemanticSurface"/> rows (Phase 4 block-aware preview reads).
    /// </summary>
    /// <remarks>
    /// Mirrors the string conventions used when reading <see cref="AudioOverlayFieldTarget"/> previews in Models (empty strings for absent fields);
    /// callers should normally build surfaces with <see cref="AudioTagSemanticSurface.FromOverlay"/>.
    /// </remarks>
    public static class AudioOverlaySemanticFieldStrings
    {
        /// <summary>
        /// Formats <paramref name="field"/> using <paramref name="semantic"/> snapshot values (empty strings replace absent semantics).
        /// </summary>
        /// <param name="semantic">Projected semantics; typically from <see cref="AudioTagSemanticSurface.FromOverlay"/>.</param>
        /// <param name="field">Which embedded field to format.</param>
        /// <returns>Invariant-compatible string suitable for previews and substring filters.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="field"/> is unrecognized.</exception>
        public static string Format(AudioTagSemanticSurface semantic, AudioOverlayField field)
        {
            return field switch
            {
                AudioOverlayField.Title => semantic.Title ?? string.Empty,
                AudioOverlayField.Album => semantic.Album ?? string.Empty,
                AudioOverlayField.Performers => semantic.Performers ?? string.Empty,
                AudioOverlayField.AlbumArtists => semantic.AlbumArtists ?? string.Empty,
                AudioOverlayField.Composers => semantic.Composers ?? string.Empty,
                AudioOverlayField.Genre => semantic.Genre ?? string.Empty,
                AudioOverlayField.Comment => semantic.Comment ?? string.Empty,
                AudioOverlayField.Lyrics => semantic.Lyrics ?? string.Empty,
                AudioOverlayField.Copyright => semantic.Copyright ?? string.Empty,
                AudioOverlayField.Grouping => semantic.Grouping ?? string.Empty,
                AudioOverlayField.Year => _InvariantUintOrEmpty(semantic.Year),
                AudioOverlayField.Track => _InvariantUintOrEmpty(semantic.Track),
                AudioOverlayField.TrackCount => _InvariantUintOrEmpty(semantic.TrackCount),
                AudioOverlayField.Disc => _InvariantUintOrEmpty(semantic.Disc),
                AudioOverlayField.DiscCount => _InvariantUintOrEmpty(semantic.DiscCount),
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
            };
        }

        private static string _InvariantUintOrEmpty(uint? value)
        {
            return value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
