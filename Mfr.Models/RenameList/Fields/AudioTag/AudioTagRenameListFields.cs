using Mfr.Models.Rename;
using Mfr.Models.Tags;

namespace Mfr.Models.RenameList.Fields.AudioTag
{
    /// <summary>
    /// All MFR7 Audio Tag Rename List fields (original columns only; preview deferred to Phase 7b).
    /// </summary>
    public static class AudioTagRenameListFields
    {
        /// <summary>
        /// MFR7 Audio Tag property group id.
        /// </summary>
        public const string Group = "MediaTag";

        /// <summary>
        /// User-visible group label in the field shuttle dropdown.
        /// </summary>
        public const string GroupLabel = "Audio Tag";

        /// <summary>
        /// Audio Tag group fields in MFR7 alphabetical display-name order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new AudioTagSemanticRenameListField("AlbumArtists", "Album Artists", SemanticAudioField.AlbumArtists),
            new AudioTagSemanticRenameListField("Album", "Album", SemanticAudioField.Album),
            new AudioTagSemanticRenameListField("AmazonId", "Amazon ID", SemanticAudioField.AmazonId),
            new AudioTagSemanticRenameListField(
                "BeatsPerMinute",
                "Beats Per Minute",
                SemanticAudioField.BeatsPerMinute
            ),
            new AudioTagSemanticRenameListField("Comment", "Comment", SemanticAudioField.Comment),
            new AudioTagSemanticRenameListField("Composers", "Composers", SemanticAudioField.Composers),
            new AudioTagSemanticRenameListField("Conductor", "Conductor", SemanticAudioField.Conductor),
            new AudioTagSemanticRenameListField("Copyright", "Copyright", SemanticAudioField.Copyright),
            new AudioTagSemanticRenameListField("DiscCount", "Disc Count", SemanticAudioField.DiscCount),
            new AudioTagSemanticRenameListField("Disc", "Disc", SemanticAudioField.Disc),
            new AudioTagFirstSegmentRenameListField(
                "FirstAlbumArtist",
                "AlbumArtist",
                SemanticAudioField.AlbumArtists
            ),
            new AudioTagFirstSegmentRenameListField("FirstComposer", "Composer", SemanticAudioField.Composers),
            new AudioTagFirstSegmentRenameListField("FirstGenre", "Genre", SemanticAudioField.Genre),
            new AudioTagFirstSegmentRenameListField("FirstPerformer", "Performer", SemanticAudioField.Performers),
            new AudioTagSemanticRenameListField("Genres", "Genres", SemanticAudioField.Genre),
            new AudioTagSemanticRenameListField("Grouping", "Grouping", SemanticAudioField.Grouping),
            new AudioTagSemanticRenameListField("Lyrics", "Lyrics", SemanticAudioField.Lyrics),
            new AudioTagSemanticRenameListField(
                "MusicBrainzArtistId",
                "Music Brainz Artist ID",
                SemanticAudioField.MusicBrainzArtistId
            ),
            new AudioTagSemanticRenameListField(
                "MusicBrainzDiscId",
                "Music Brainz Disc ID",
                SemanticAudioField.MusicBrainzDiscId
            ),
            new AudioTagSemanticRenameListField(
                "MusicBrainzReleaseArtistId",
                "Music Brainz Release Artist ID",
                SemanticAudioField.MusicBrainzReleaseArtistId
            ),
            new AudioTagSemanticRenameListField(
                "MusicBrainzReleaseCountry",
                "Music Brainz Release Country",
                SemanticAudioField.MusicBrainzReleaseCountry
            ),
            new AudioTagSemanticRenameListField(
                "MusicBrainzReleaseId",
                "Music Brainz Release ID",
                SemanticAudioField.MusicBrainzReleaseId
            ),
            new AudioTagSemanticRenameListField(
                "MusicBrainzReleaseStatus",
                "Music Brainz Release Status",
                SemanticAudioField.MusicBrainzReleaseStatus
            ),
            new AudioTagSemanticRenameListField(
                "MusicBrainzReleaseType",
                "Music Brainz Release Type",
                SemanticAudioField.MusicBrainzReleaseType
            ),
            new AudioTagSemanticRenameListField(
                "MusicBrainzTrackId",
                "Music Brainz Track ID",
                SemanticAudioField.MusicBrainzTrackId
            ),
            new AudioTagSemanticRenameListField("MusicIpId", "MusicIP ID", SemanticAudioField.MusicIpId),
            new AudioTagSemanticRenameListField("Performers", "Performers", SemanticAudioField.Performers),
            new AudioTagTagTypesField(),
            new AudioTagSemanticRenameListField("Title", "Title", SemanticAudioField.Title),
            new AudioTagSemanticRenameListField("TrackCount", "Track Count", SemanticAudioField.TrackCount),
            new AudioTagSemanticRenameListField("Track", "Track", SemanticAudioField.Track, defaultWidth: 40),
            new AudioTagSemanticRenameListField("Year", "Year", SemanticAudioField.Year, defaultWidth: 60),
        ];
    }

    internal sealed class AudioTagTagTypesField() : AudioTagRenameListField("TagTypes", "Tag Types")
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
