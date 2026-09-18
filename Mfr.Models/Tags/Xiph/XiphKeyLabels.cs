namespace Mfr.Models.Tags.Xiph
{
    /// <summary>
    /// User-visible Apply-To labels and tips for modeled Xiph comment keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One static map owns every known key: semantic common fields, Xiph-only distinct wording
    /// (e.g. Track Number vs Track), and catalog/MusicBrainz rows from <see cref="AudioCatalogFieldMaps"/>.
    /// Every known key has a non-empty tip.
    /// </para>
    /// </remarks>
    public static class XiphKeyLabels
    {
        private readonly record struct LabelTip(string Label, string Tip);

        private static readonly Dictionary<string, LabelTip> s_KeyToLabelTip = _BuildMap();

        /// <summary>
        /// Returns the Apply-To label for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Xiph comment key (any casing).</param>
        /// <returns>Friendly label, or the key when unrecognized.</returns>
        public static string For(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var normalized = key.Trim().ToUpperInvariant();
            return s_KeyToLabelTip.TryGetValue(normalized, out var entry) ? entry.Label : key;
        }

        /// <summary>
        /// Returns the tip for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Xiph comment key (any casing).</param>
        /// <returns>Clarifying tip, or <see langword="null"/> when the key is unrecognized.</returns>
        public static string? Tip(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var normalized = key.Trim().ToUpperInvariant();
            return s_KeyToLabelTip.TryGetValue(normalized, out var entry) ? entry.Tip : null;
        }

        private static Dictionary<string, LabelTip> _BuildMap()
        {
            var keyToLabelTip = new Dictionary<string, LabelTip>(StringComparer.Ordinal);

            void Add(string key, string label, string tip)
            {
                keyToLabelTip[key] = new LabelTip(label, tip);
            }

            void AddSemantic(string key, SemanticAudioField field)
            {
                Add(key, SemanticAudioFieldLabels.For(field), SemanticAudioFieldTips.For(field));
            }

            AddSemantic(XiphKnownKeys.Title, SemanticAudioField.Title);
            AddSemantic(XiphKnownKeys.Album, SemanticAudioField.Album);
            AddSemantic(XiphKnownKeys.Artist, SemanticAudioField.Performers);
            AddSemantic(XiphKnownKeys.AlbumArtist, SemanticAudioField.AlbumArtists);
            AddSemantic(XiphKnownKeys.Composer, SemanticAudioField.Composers);
            AddSemantic(XiphKnownKeys.Genre, SemanticAudioField.Genre);
            AddSemantic(XiphKnownKeys.Comment, SemanticAudioField.Comment);
            AddSemantic(XiphKnownKeys.Lyrics, SemanticAudioField.Lyrics);
            AddSemantic(XiphKnownKeys.Copyright, SemanticAudioField.Copyright);
            AddSemantic(XiphKnownKeys.Grouping, SemanticAudioField.Grouping);
            AddSemantic(XiphKnownKeys.Year, SemanticAudioField.Year);
            AddSemantic(XiphKnownKeys.BeatsPerMinute, SemanticAudioField.BeatsPerMinute);
            AddSemantic(XiphKnownKeys.Conductor, SemanticAudioField.Conductor);

            Add(
                XiphKnownKeys.Description,
                "Description",
                "Preferred Xiph comment/notes field (DESCRIPTION); preferred over COMMENT for semantic merge."
            );
            Add(
                XiphKnownKeys.UnsyncedLyrics,
                "Unsynced Lyrics",
                "Unsynchronised lyrics alias (UNSYNCEDLYRICS); cleared when LYRICS is written via semantic merge."
            );
            Add(
                XiphKnownKeys.ContentGroup,
                "Content Group",
                "Content group alias (CONTENTGROUP); cleared when GROUPING is written via semantic merge."
            );
            Add(
                XiphKnownKeys.Date,
                "Date",
                "Preferred Xiph date/year field (DATE); preferred over YEAR for semantic merge."
            );
            Add(XiphKnownKeys.TrackNumber, "Track Number", "Track number within the album or disc (TRACKNUMBER).");
            Add(XiphKnownKeys.TrackTotal, "Track Total", "Total tracks on the album or disc (TRACKTOTAL).");
            Add(
                XiphKnownKeys.TotalTracks,
                "Total Tracks",
                "Total tracks alias (TOTALTRACKS); cleared when TRACKTOTAL is written via semantic merge."
            );
            Add(XiphKnownKeys.DiscNumber, "Disc Number", "Disc number in a multi-disc set (DISCNUMBER).");
            Add(XiphKnownKeys.DiscTotal, "Disc Total", "Total discs in a multi-disc set (DISCTOTAL).");
            Add(
                XiphKnownKeys.TotalDiscs,
                "Total Discs",
                "Total discs alias (TOTALDISCS); cleared when DISCTOTAL is written via semantic merge."
            );
            Add(XiphKnownKeys.Tempo, "Tempo", "Tempo alias (TEMPO); cleared when BPM is written via semantic merge.");

            foreach (var row in AudioCatalogFieldMaps.All)
            {
                AddSemantic(row.XiphKey, row.Field);
            }

            return keyToLabelTip;
        }
    }
}
