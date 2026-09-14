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
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>Album title.</summary>
            public const string Album = "Album";

            /// <summary>Album artists (joined).</summary>
            public const string AlbumArtists = "AlbumArtists";

            /// <summary>First album-artist segment.</summary>
            public const string FirstAlbumArtist = "FirstAlbumArtist";

            /// <summary>Performers / artists (joined).</summary>
            public const string Performers = "Performers";

            /// <summary>First performer segment.</summary>
            public const string FirstPerformer = "FirstPerformer";

            /// <summary>Amazon ASIN.</summary>
            public const string AmazonId = "AmazonId";

            /// <summary>Beats per minute.</summary>
            public const string BeatsPerMinute = "BeatsPerMinute";

            /// <summary>Comment.</summary>
            public const string Comment = "Comment";

            /// <summary>Composers (joined).</summary>
            public const string Composers = "Composers";

            /// <summary>First composer segment.</summary>
            public const string FirstComposer = "FirstComposer";

            /// <summary>Conductor.</summary>
            public const string Conductor = "Conductor";

            /// <summary>Copyright.</summary>
            public const string Copyright = "Copyright";

            /// <summary>Disc number.</summary>
            public const string Disc = "Disc";

            /// <summary>Disc count.</summary>
            public const string DiscCount = "DiscCount";

            /// <summary>Genres (joined); catalog key for <see cref="SemanticAudioField.Genre"/>.</summary>
            public const string Genres = "Genres";

            /// <summary>First genre segment.</summary>
            public const string FirstGenre = "FirstGenre";

            /// <summary>Grouping.</summary>
            public const string Grouping = "Grouping";

            /// <summary>Lyrics.</summary>
            public const string Lyrics = "Lyrics";

            /// <summary>MusicBrainz release artist id.</summary>
            public const string MusicBrainzReleaseArtistId = "MusicBrainzReleaseArtistId";

            /// <summary>MusicBrainz release id.</summary>
            public const string MusicBrainzReleaseId = "MusicBrainzReleaseId";

            /// <summary>MusicBrainz release country.</summary>
            public const string MusicBrainzReleaseCountry = "MusicBrainzReleaseCountry";

            /// <summary>MusicBrainz release status.</summary>
            public const string MusicBrainzReleaseStatus = "MusicBrainzReleaseStatus";

            /// <summary>MusicBrainz release type.</summary>
            public const string MusicBrainzReleaseType = "MusicBrainzReleaseType";

            /// <summary>MusicBrainz artist id.</summary>
            public const string MusicBrainzArtistId = "MusicBrainzArtistId";

            /// <summary>MusicBrainz disc id.</summary>
            public const string MusicBrainzDiscId = "MusicBrainzDiscId";

            /// <summary>MusicBrainz track id.</summary>
            public const string MusicBrainzTrackId = "MusicBrainzTrackId";

            /// <summary>MusicIP PUID.</summary>
            public const string MusicIpId = "MusicIpId";

            /// <summary>Title.</summary>
            public const string Title = "Title";

            /// <summary>Track number.</summary>
            public const string Track = "Track";

            /// <summary>Track count.</summary>
            public const string TrackCount = "TrackCount";

            /// <summary>Year.</summary>
            public const string Year = "Year";
        }

        private static readonly Lazy<Dictionary<SemanticAudioField, string>> _semanticFieldToPropertyKey = new(
            _BuildSemanticFieldToPropertyKey
        );

        /// <summary>
        /// Audio Tag group fields: parent + <c>(first)</c> clusters, then display-label order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            _Semantic(Key.Album, SemanticAudioField.Album, defaultWidth: 200),
            _Semantic(Key.AlbumArtists, SemanticAudioField.AlbumArtists, defaultWidth: 200),
            _First(Key.FirstAlbumArtist, SemanticAudioField.AlbumArtists),
            _Semantic(Key.Performers, SemanticAudioField.Performers, defaultWidth: 200),
            _First(Key.FirstPerformer, SemanticAudioField.Performers),
            _Semantic(Key.AmazonId, SemanticAudioField.AmazonId),
            _Semantic(Key.BeatsPerMinute, SemanticAudioField.BeatsPerMinute),
            _Semantic(Key.Comment, SemanticAudioField.Comment, defaultWidth: 200),
            _Semantic(Key.Composers, SemanticAudioField.Composers),
            _First(Key.FirstComposer, SemanticAudioField.Composers),
            _Semantic(Key.Conductor, SemanticAudioField.Conductor),
            _Semantic(Key.Copyright, SemanticAudioField.Copyright, defaultWidth: 200),
            _Semantic(Key.Disc, SemanticAudioField.Disc),
            _Semantic(Key.DiscCount, SemanticAudioField.DiscCount),
            _Semantic(Key.Genres, SemanticAudioField.Genre),
            _First(Key.FirstGenre, SemanticAudioField.Genre),
            _Semantic(Key.Grouping, SemanticAudioField.Grouping),
            _Semantic(Key.Lyrics, SemanticAudioField.Lyrics, defaultWidth: 200),
            _Semantic(Key.MusicBrainzReleaseArtistId, SemanticAudioField.MusicBrainzReleaseArtistId),
            _Semantic(Key.MusicBrainzReleaseId, SemanticAudioField.MusicBrainzReleaseId),
            _Semantic(Key.MusicBrainzReleaseCountry, SemanticAudioField.MusicBrainzReleaseCountry),
            _Semantic(Key.MusicBrainzReleaseStatus, SemanticAudioField.MusicBrainzReleaseStatus),
            _Semantic(Key.MusicBrainzReleaseType, SemanticAudioField.MusicBrainzReleaseType),
            _Semantic(Key.MusicBrainzArtistId, SemanticAudioField.MusicBrainzArtistId),
            _Semantic(Key.MusicBrainzDiscId, SemanticAudioField.MusicBrainzDiscId),
            _Semantic(Key.MusicBrainzTrackId, SemanticAudioField.MusicBrainzTrackId),
            _Semantic(Key.MusicIpId, SemanticAudioField.MusicIpId),
            new AudioTagTagTypesField(),
            _Semantic(Key.Title, SemanticAudioField.Title, defaultWidth: 200),
            _Semantic(Key.Track, SemanticAudioField.Track, defaultWidth: 40),
            _Semantic(Key.TrackCount, SemanticAudioField.TrackCount),
            _Semantic(Key.Year, SemanticAudioField.Year, defaultWidth: 60),
        ];

        /// <summary>
        /// Maps a semantic audio field to its catalog property key (e.g. Genre → Genres).
        /// </summary>
        /// <param name="field">Semantic field from the audio tag overlay.</param>
        /// <param name="propertyKey">Catalog property key when mapped.</param>
        /// <returns><see langword="true"/> when <paramref name="field"/> has a semantic Rename List column.</returns>
        public static bool TryGetSemanticPropertyKey(SemanticAudioField field, out string propertyKey)
        {
            return _semanticFieldToPropertyKey.Value.TryGetValue(field, out propertyKey!);
        }

        /// <summary>
        /// Creates a semantic column with the shared display label and optional clarifying tooltip.
        /// </summary>
        private static AudioTagSemanticRenameListField _Semantic(
            string propertyKey,
            SemanticAudioField field,
            int defaultWidth = 160
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

        private static Dictionary<SemanticAudioField, string> _BuildSemanticFieldToPropertyKey()
        {
            var fieldToPropertyKey = new Dictionary<SemanticAudioField, string>();
            foreach (var field in All.OfType<AudioTagSemanticRenameListField>())
            {
                fieldToPropertyKey.Add(field.Field, field.PropertyKey);
            }

            return fieldToPropertyKey;
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
