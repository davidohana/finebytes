using Mfr.Models.Rename;
using Mfr.Models.Tags;

namespace Mfr.Models.RenameList.Fields.AudioTag
{
    /// <summary>
    /// All MFR7 Audio Tag Rename List fields.
    /// </summary>
    /// <remarks>
    /// Semantic fields are <c>ReadWriteApply</c> (previewable). First-segment and Tag Types stay
    /// original-only (<c>ReadOnly</c>), matching MFR7 <c>AudioTagPgInfo</c>.
    /// Display names come from <see cref="SemanticAudioFieldLabels"/>. Catalog order keeps each
    /// multi-value field next to its <c>(first)</c> column, then follows display-label order
    /// (with Album Artist / Artist clustered at the top).
    /// </remarks>
    public static class AudioTagRenameListFields
    {
        /// <summary>
        /// MFR7 Audio Tag property group id.
        /// </summary>
        public const string Group = "MediaTag";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "Audio Tag";

        /// <summary>
        /// Audio Tag group fields: parent + <c>(first)</c> clusters, then display-label order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            _Semantic("Album", SemanticAudioField.Album),
            _Semantic("AlbumArtists", SemanticAudioField.AlbumArtists),
            _First("FirstAlbumArtist", SemanticAudioField.AlbumArtists),
            _Semantic("Performers", SemanticAudioField.Performers),
            _First("FirstPerformer", SemanticAudioField.Performers),
            _Semantic("AmazonId", SemanticAudioField.AmazonId),
            _Semantic("BeatsPerMinute", SemanticAudioField.BeatsPerMinute),
            _Semantic("Comment", SemanticAudioField.Comment),
            _Semantic("Composers", SemanticAudioField.Composers),
            _First("FirstComposer", SemanticAudioField.Composers),
            _Semantic("Conductor", SemanticAudioField.Conductor),
            _Semantic("Copyright", SemanticAudioField.Copyright),
            _Semantic("Disc", SemanticAudioField.Disc),
            _Semantic("DiscCount", SemanticAudioField.DiscCount),
            _Semantic("Genres", SemanticAudioField.Genre),
            _First("FirstGenre", SemanticAudioField.Genre),
            _Semantic("Grouping", SemanticAudioField.Grouping),
            _Semantic("Lyrics", SemanticAudioField.Lyrics),
            _Semantic("MusicBrainzReleaseArtistId", SemanticAudioField.MusicBrainzReleaseArtistId),
            _Semantic("MusicBrainzReleaseId", SemanticAudioField.MusicBrainzReleaseId),
            _Semantic("MusicBrainzReleaseCountry", SemanticAudioField.MusicBrainzReleaseCountry),
            _Semantic("MusicBrainzReleaseStatus", SemanticAudioField.MusicBrainzReleaseStatus),
            _Semantic("MusicBrainzReleaseType", SemanticAudioField.MusicBrainzReleaseType),
            _Semantic("MusicBrainzArtistId", SemanticAudioField.MusicBrainzArtistId),
            _Semantic("MusicBrainzDiscId", SemanticAudioField.MusicBrainzDiscId),
            _Semantic("MusicBrainzTrackId", SemanticAudioField.MusicBrainzTrackId),
            _Semantic("MusicIpId", SemanticAudioField.MusicIpId),
            new AudioTagTagTypesField(),
            _Semantic("Title", SemanticAudioField.Title),
            _Semantic("Track", SemanticAudioField.Track, defaultWidth: 40),
            _Semantic("TrackCount", SemanticAudioField.TrackCount),
            _Semantic("Year", SemanticAudioField.Year, defaultWidth: 60),
        ];

        /// <summary>
        /// Creates a semantic column with the shared display label and optional clarifying tooltip.
        /// </summary>
        private static AudioTagSemanticRenameListField _Semantic(
            string propertyKey,
            SemanticAudioField field,
            int defaultWidth = 100
        )
        {
            return new AudioTagSemanticRenameListField(
                propertyKey,
                SemanticAudioFieldLabels.For(field),
                field,
                defaultWidth,
                SemanticAudioFieldTips.For(field)
            );
        }

        /// <summary>
        /// Creates a first-segment column with the shared first-segment label and tooltip.
        /// </summary>
        private static AudioTagFirstSegmentRenameListField _First(string propertyKey, SemanticAudioField field)
        {
            return new AudioTagFirstSegmentRenameListField(
                propertyKey,
                SemanticAudioFieldLabels.FirstSegment(field),
                field,
                SemanticAudioFieldTips.FirstSegment(field)
            );
        }
    }

    internal sealed class AudioTagTagTypesField()
        : AudioTagRenameListField(
            "TagTypes",
            "Tag Types",
            tip: "Which embedded tag blocks are present (e.g. Id3v2;Xiph)."
        )
    {
        public override string Resolve(FileMeta meta)
        {
            var kinds = meta.AudioTagOverlay.GetPresentBlockKinds();
            if (kinds.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(';', kinds);
        }
    }
}
