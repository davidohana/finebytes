using System.Globalization;
using Mfr.Models.Tags;

namespace Mfr.Metadata
{
    /// <summary>
    /// Filter/preview field strings from <see cref="SemanticAudioTag"/> rows.
    /// </summary>
    /// <remarks>
    /// Mirrors the string conventions used when reading <see cref="Models.AudioFieldTarget"/> previews (empty strings for absent fields);
    /// callers should normally build values with <see cref="SemanticAudioTag.FromOverlay"/>.
    /// </remarks>
    public static class SemanticAudioFieldStrings
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

        private static string _DecimalDigitsOrEmpty(uint? value)
        {
            return value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
