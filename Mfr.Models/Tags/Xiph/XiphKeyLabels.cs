namespace Mfr.Models.Tags.Xiph
{
    /// <summary>
    /// User-visible Apply-To labels and tips for modeled Xiph comment keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One static map owns every known key: semantic common fields, Xiph-only distinct wording
    /// (e.g. Track Number vs Track), and catalog/MusicBrainz rows from <see cref="AudioCatalogFieldMaps"/>.
    /// </para>
    /// </remarks>
    public static class XiphKeyLabels
    {
        private readonly record struct LabelTip(string Label, string? Tip);

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
        /// Returns an optional Apply-To tip for <paramref name="key"/> when the label alone is ambiguous.
        /// </summary>
        /// <param name="key">Xiph comment key (any casing).</param>
        /// <returns>Clarifying tip, or <see langword="null"/> when quiet.</returns>
        /// <remarks>
        /// <para>
        /// Most catalog-ID keys stay quiet — their labels are already specific. Abbreviation
        /// keys (ASIN, BPM) and role / multi-value common keys get tips.
        /// </para>
        /// </remarks>
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

            void Add(string key, string label, string? tip = null)
            {
                keyToLabelTip[key] = new LabelTip(label, tip);
            }

            void AddSemantic(string key, SemanticAudioField field, string? tip = null)
            {
                Add(key, SemanticAudioFieldLabels.For(field), tip);
            }

            AddSemantic(XiphKnownKeys.Title, SemanticAudioField.Title);
            AddSemantic(XiphKnownKeys.Album, SemanticAudioField.Album);
            AddSemantic(XiphKnownKeys.Artist, SemanticAudioField.Performers, SemanticAudioFieldTips.Artist);
            AddSemantic(XiphKnownKeys.AlbumArtist, SemanticAudioField.AlbumArtists, SemanticAudioFieldTips.AlbumArtist);
            AddSemantic(XiphKnownKeys.Composer, SemanticAudioField.Composers, SemanticAudioFieldTips.Composer);
            AddSemantic(XiphKnownKeys.Genre, SemanticAudioField.Genre, SemanticAudioFieldTips.Genre);
            AddSemantic(XiphKnownKeys.Comment, SemanticAudioField.Comment);
            AddSemantic(XiphKnownKeys.Lyrics, SemanticAudioField.Lyrics);
            AddSemantic(XiphKnownKeys.Copyright, SemanticAudioField.Copyright);
            AddSemantic(XiphKnownKeys.Grouping, SemanticAudioField.Grouping);
            AddSemantic(XiphKnownKeys.Year, SemanticAudioField.Year);
            AddSemantic(XiphKnownKeys.BeatsPerMinute, SemanticAudioField.BeatsPerMinute, SemanticAudioFieldTips.Bpm);
            AddSemantic(XiphKnownKeys.Conductor, SemanticAudioField.Conductor);

            Add(XiphKnownKeys.Description, "Description");
            Add(XiphKnownKeys.UnsyncedLyrics, "Unsynced Lyrics");
            Add(XiphKnownKeys.ContentGroup, "Content Group");
            Add(XiphKnownKeys.Date, "Date");
            Add(XiphKnownKeys.TrackNumber, "Track Number");
            Add(XiphKnownKeys.TrackTotal, "Track Total");
            Add(XiphKnownKeys.TotalTracks, "Total Tracks");
            Add(XiphKnownKeys.DiscNumber, "Disc Number");
            Add(XiphKnownKeys.DiscTotal, "Disc Total");
            Add(XiphKnownKeys.TotalDiscs, "Total Discs");
            Add(XiphKnownKeys.Tempo, "Tempo");

            foreach (var row in AudioCatalogFieldMaps.All)
            {
                var tip = row.Field == SemanticAudioField.AmazonId ? SemanticAudioFieldTips.Asin : null;
                AddSemantic(row.XiphKey, row.Field, tip);
            }

            return keyToLabelTip;
        }
    }
}
