using System.Diagnostics.CodeAnalysis;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// User-facing tooltips for <see cref="SemanticAudioField"/> (Rename List, Apply-To, Format Editor).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Clarifies roles that share similar labels (Artist vs Album Artist) and multi-value
    /// <c>;</c>-joined fields vs first-segment Rename List columns.
    /// </para>
    /// </remarks>
    public static class SemanticAudioFieldTips
    {
        /// <summary>Tooltip for <see cref="SemanticAudioField.Performers"/> (display label Artist).</summary>
        public const string Artist = "Track performers. Multiple values are joined with `;` (e.g. Alice; Bob).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.AlbumArtists"/>.</summary>
        public const string AlbumArtist =
            "Album-level credit (compilations / Various Artists). Distinct from Artist. Multiple values joined with `;`.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Composers"/>.</summary>
        public const string Composer = "Composer(s). Multiple values are joined with `;`.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Genre"/>.</summary>
        public const string Genre = "Genre. Multiple values are joined with `;`.";

        /// <summary>
        /// Returns the shared tooltip for <paramref name="field"/>.
        /// </summary>
        /// <param name="field">Semantic field.</param>
        /// <returns>User-visible tooltip text.</returns>
        public static string For(SemanticAudioField field)
        {
            return field switch
            {
                SemanticAudioField.Performers => Artist,
                SemanticAudioField.AlbumArtists => AlbumArtist,
                SemanticAudioField.Composers => Composer,
                SemanticAudioField.Genre => Genre,
                SemanticAudioField.Title => "Track title.",
                SemanticAudioField.Album => "Album name.",
                SemanticAudioField.Comment => "Comment text.",
                SemanticAudioField.Lyrics => "Lyrics text.",
                SemanticAudioField.Copyright => "Copyright notice.",
                SemanticAudioField.Grouping => "Content grouping.",
                SemanticAudioField.Year => "Release year.",
                SemanticAudioField.Track => "Track number.",
                SemanticAudioField.TrackCount => "Total tracks on the disc or album.",
                SemanticAudioField.Disc => "Disc number.",
                SemanticAudioField.DiscCount => "Total discs in the set.",
                SemanticAudioField.BeatsPerMinute => "Tempo in beats per minute.",
                SemanticAudioField.Conductor => "Conductor or director.",
                SemanticAudioField.MusicBrainzArtistId => "MusicBrainz artist ID.",
                SemanticAudioField.MusicBrainzReleaseId => "MusicBrainz release (album) ID.",
                SemanticAudioField.MusicBrainzReleaseArtistId => "MusicBrainz album artist ID.",
                SemanticAudioField.MusicBrainzTrackId => "MusicBrainz track ID.",
                SemanticAudioField.MusicBrainzDiscId => "MusicBrainz disc ID.",
                SemanticAudioField.MusicBrainzReleaseStatus => "MusicBrainz release status.",
                SemanticAudioField.MusicBrainzReleaseType => "MusicBrainz release type.",
                SemanticAudioField.MusicBrainzReleaseCountry => "MusicBrainz release country.",
                SemanticAudioField.MusicIpId => "MusicIP PUID.",
                SemanticAudioField.AmazonId => "Amazon ASIN.",
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown semantic audio field."),
            };
        }

        /// <summary>
        /// Returns the tooltip for a first-segment Rename List column of a multi-value field.
        /// </summary>
        /// <param name="field">Semantic field whose first delimited segment is shown.</param>
        /// <returns>User-visible tooltip text.</returns>
        [SuppressMessage(
            "Style",
            "IDE0072:Add missing cases",
            Justification = "Only multi-value fields expose first-segment Rename List columns."
        )]
        public static string FirstSegment(SemanticAudioField field)
        {
            var label = SemanticAudioFieldLabels.For(field);
            return field switch
            {
                SemanticAudioField.Performers
                or SemanticAudioField.AlbumArtists
                or SemanticAudioField.Composers
                or SemanticAudioField.Genre =>
                    $"Only the first {label} value before `;`. Same tag as {label}; original-only column.",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(field),
                    field,
                    "First-segment tips are only defined for multi-value semantic fields."
                ),
            };
        }
    }
}
