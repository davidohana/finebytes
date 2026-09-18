namespace Mfr.Models.Tags
{
    /// <summary>
    /// User-facing tooltips for every <see cref="SemanticAudioField"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by Audio Tag Apply-To and Rename List columns. Multi-value / role-confused fields and
    /// abbreviations get the longest explanations; simpler fields still get a one-line meaning.
    /// </para>
    /// </remarks>
    public static class SemanticAudioFieldTips
    {
        /// <summary>Tooltip for <see cref="SemanticAudioField.Title"/>.</summary>
        public const string Title = "Track title.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Album"/>.</summary>
        public const string Album = "Album / release title.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Performers"/> (display label Artist).</summary>
        public const string Artist = "Track performers. Multiple values are joined with `;` (e.g. Alice; Bob).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.AlbumArtists"/>.</summary>
        public const string AlbumArtist =
            "Album-level credit (compilations / Various Artists). Distinct from Artist. Multiple values joined with `;`.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Composers"/>.</summary>
        public const string Composer = "Composer(s). Multiple values are joined with `;`.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Genre"/>.</summary>
        public const string Genre = "Genre. Multiple values are joined with `;`.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Comment"/>.</summary>
        public const string Comment = "Free-text comment / notes for the track.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Lyrics"/>.</summary>
        public const string Lyrics = "Song lyrics text.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Copyright"/>.</summary>
        public const string Copyright = "Copyright notice for the recording or release.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Grouping"/>.</summary>
        public const string Grouping = "Content group / work set (playlist or classical work grouping).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Year"/>.</summary>
        public const string Year = "Release or recording year.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Track"/>.</summary>
        public const string Track = "Track number within the album (or disc).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.TrackCount"/>.</summary>
        public const string TrackCount = "Total number of tracks on the album (or disc).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Disc"/>.</summary>
        public const string Disc = "Disc number in a multi-disc set.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.DiscCount"/>.</summary>
        public const string DiscCount = "Total number of discs in a multi-disc set.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.BeatsPerMinute"/> (display label BPM).</summary>
        public const string Bpm = "Tempo in beats per minute.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.Conductor"/>.</summary>
        public const string Conductor = "Conductor or musical director.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzArtistId"/>.</summary>
        public const string MusicBrainzArtistId = "MusicBrainz artist UUID (Picard / MusicBrainz tagging).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzReleaseId"/>.</summary>
        public const string MusicBrainzReleaseId = "MusicBrainz release (album) UUID.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzReleaseArtistId"/>.</summary>
        public const string MusicBrainzReleaseArtistId = "MusicBrainz release artist (album artist) UUID.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzTrackId"/>.</summary>
        public const string MusicBrainzTrackId = "MusicBrainz recording / track UUID.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzDiscId"/>.</summary>
        public const string MusicBrainzDiscId = "MusicBrainz disc ID (TOC fingerprint).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzReleaseStatus"/>.</summary>
        public const string MusicBrainzReleaseStatus = "MusicBrainz release status (e.g. official, promotion).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzReleaseType"/>.</summary>
        public const string MusicBrainzReleaseType = "MusicBrainz release type (e.g. album, single, EP).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicBrainzReleaseCountry"/>.</summary>
        public const string MusicBrainzReleaseCountry = "MusicBrainz release country code.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.MusicIpId"/>.</summary>
        public const string MusicIpId = "MusicIP acoustic fingerprint PUID.";

        /// <summary>Tooltip for <see cref="SemanticAudioField.AmazonId"/> (display label ASIN).</summary>
        public const string Asin = "Amazon Standard Identification Number (product ASIN).";

        /// <summary>
        /// Returns the tooltip for <paramref name="field"/>.
        /// </summary>
        /// <param name="field">Semantic field.</param>
        /// <returns>User-visible tooltip text.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="field"/> is not mapped.</exception>
        public static string For(SemanticAudioField field)
        {
            return field switch
            {
                SemanticAudioField.Title => Title,
                SemanticAudioField.Album => Album,
                SemanticAudioField.Performers => Artist,
                SemanticAudioField.AlbumArtists => AlbumArtist,
                SemanticAudioField.Composers => Composer,
                SemanticAudioField.Genre => Genre,
                SemanticAudioField.Comment => Comment,
                SemanticAudioField.Lyrics => Lyrics,
                SemanticAudioField.Copyright => Copyright,
                SemanticAudioField.Grouping => Grouping,
                SemanticAudioField.Year => Year,
                SemanticAudioField.Track => Track,
                SemanticAudioField.TrackCount => TrackCount,
                SemanticAudioField.Disc => Disc,
                SemanticAudioField.DiscCount => DiscCount,
                SemanticAudioField.BeatsPerMinute => Bpm,
                SemanticAudioField.Conductor => Conductor,
                SemanticAudioField.MusicBrainzArtistId => MusicBrainzArtistId,
                SemanticAudioField.MusicBrainzReleaseId => MusicBrainzReleaseId,
                SemanticAudioField.MusicBrainzReleaseArtistId => MusicBrainzReleaseArtistId,
                SemanticAudioField.MusicBrainzTrackId => MusicBrainzTrackId,
                SemanticAudioField.MusicBrainzDiscId => MusicBrainzDiscId,
                SemanticAudioField.MusicBrainzReleaseStatus => MusicBrainzReleaseStatus,
                SemanticAudioField.MusicBrainzReleaseType => MusicBrainzReleaseType,
                SemanticAudioField.MusicBrainzReleaseCountry => MusicBrainzReleaseCountry,
                SemanticAudioField.MusicIpId => MusicIpId,
                SemanticAudioField.AmazonId => Asin,
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Missing semantic field tip."),
            };
        }

        /// <summary>
        /// Returns the tooltip for a first-segment Rename List column of a multi-value field.
        /// </summary>
        /// <param name="field">Semantic field whose first delimited segment is shown.</param>
        /// <returns>User-visible tooltip text.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="field"/> is not a multi-value first-segment column.</exception>
        public static string FirstSegment(SemanticAudioField field)
        {
            // Only multi-value semantic columns expose a (first) Rename List twin.
            if (
                field
                is not (
                    SemanticAudioField.Performers
                    or SemanticAudioField.AlbumArtists
                    or SemanticAudioField.Composers
                    or SemanticAudioField.Genre
                )
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(field),
                    field,
                    "First-segment tips are only defined for multi-value semantic fields."
                );
            }

            var label = SemanticAudioFieldLabels.For(field);
            return $"Only the first {label} value before `;`. Same tag as {label}; original-only column.";
        }
    }
}
