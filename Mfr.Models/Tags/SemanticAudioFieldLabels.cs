using System.Diagnostics.CodeAnalysis;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// User-facing display labels for <see cref="SemanticAudioField"/> (Apply-To, Rename List, Format Editor).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Catalog/MusicBrainz fields reuse Picard TXXX descriptions from <see cref="AudioCatalogFieldMaps"/>.
    /// Common fields use singular player-facing names (Artist, Album Artist, Genre, BPM, …).
    /// </para>
    /// </remarks>
    public static class SemanticAudioFieldLabels
    {
        private static readonly Dictionary<SemanticAudioField, string> s_FieldToLabel = _BuildMap();

        /// <summary>
        /// Returns the shared display label for <paramref name="field"/>.
        /// </summary>
        /// <param name="field">Semantic field.</param>
        /// <returns>User-visible label.</returns>
        public static string For(SemanticAudioField field)
        {
            return s_FieldToLabel[field];
        }

        /// <summary>
        /// Returns the first-segment column label for a multi-value semantic field.
        /// </summary>
        /// <param name="field">Semantic field whose first delimited segment is shown.</param>
        /// <returns>Label of the form <c>{For(field)} (first)</c>.</returns>
        public static string FirstSegment(SemanticAudioField field)
        {
            return $"{For(field)} (first)";
        }

        /// <summary>
        /// Builds the complete field→label map (catalog rows plus common-field defaults).
        /// </summary>
        private static Dictionary<SemanticAudioField, string> _BuildMap()
        {
            var fieldToLabel = new Dictionary<SemanticAudioField, string>();
            foreach (var row in AudioCatalogFieldMaps.All)
            {
                fieldToLabel[row.Field] = row.Id3v2TxxxDescription;
            }

            foreach (var field in Enum.GetValues<SemanticAudioField>())
            {
                if (fieldToLabel.ContainsKey(field))
                {
                    continue;
                }

                fieldToLabel[field] = _CommonLabel(field);
            }

            return fieldToLabel;
        }

        /// <summary>
        /// Label for non-catalog semantic fields.
        /// </summary>
        [SuppressMessage(
            "Style",
            "IDE0072:Add missing cases",
            Justification = "Catalog fields are filled from AudioCatalogFieldMaps in _BuildMap; default throws for new non-catalog members."
        )]
        private static string _CommonLabel(SemanticAudioField field)
        {
            return field switch
            {
                SemanticAudioField.Title => "Title",
                SemanticAudioField.Album => "Album",
                SemanticAudioField.Performers => "Artist",
                SemanticAudioField.AlbumArtists => "Album Artist",
                SemanticAudioField.Composers => "Composer",
                SemanticAudioField.Genre => "Genre",
                SemanticAudioField.Comment => "Comment",
                SemanticAudioField.Lyrics => "Lyrics",
                SemanticAudioField.Copyright => "Copyright",
                SemanticAudioField.Grouping => "Grouping",
                SemanticAudioField.Year => "Year",
                SemanticAudioField.Track => "Track",
                SemanticAudioField.TrackCount => "Track Count",
                SemanticAudioField.Disc => "Disc",
                SemanticAudioField.DiscCount => "Disc Count",
                SemanticAudioField.BeatsPerMinute => "BPM",
                SemanticAudioField.Conductor => "Conductor",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(field),
                    field,
                    "Non-catalog semantic fields must have an explicit display label."
                ),
            };
        }
    }
}
