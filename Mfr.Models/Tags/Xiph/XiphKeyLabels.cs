namespace Mfr.Models.Tags.Xiph
{
    /// <summary>
    /// User-visible Apply-To labels and tips for modeled Xiph comment keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Catalog IDs follow <see cref="SemanticAudioFieldLabels"/> via <see cref="AudioCatalogFieldMaps"/>.
    /// Overlapping common keys reuse that map; Xiph-only aliases keep distinct wording
    /// (e.g. Track Number vs Track).
    /// </para>
    /// </remarks>
    public static class XiphKeyLabels
    {
        private static readonly Dictionary<string, SemanticAudioField> s_KeyToSemanticField = new(
            StringComparer.Ordinal
        )
        {
            [XiphKnownKeys.Title] = SemanticAudioField.Title,
            [XiphKnownKeys.Album] = SemanticAudioField.Album,
            [XiphKnownKeys.Artist] = SemanticAudioField.Performers,
            [XiphKnownKeys.AlbumArtist] = SemanticAudioField.AlbumArtists,
            [XiphKnownKeys.Composer] = SemanticAudioField.Composers,
            [XiphKnownKeys.Genre] = SemanticAudioField.Genre,
            [XiphKnownKeys.Comment] = SemanticAudioField.Comment,
            [XiphKnownKeys.Lyrics] = SemanticAudioField.Lyrics,
            [XiphKnownKeys.Copyright] = SemanticAudioField.Copyright,
            [XiphKnownKeys.Grouping] = SemanticAudioField.Grouping,
            [XiphKnownKeys.Year] = SemanticAudioField.Year,
            [XiphKnownKeys.BeatsPerMinute] = SemanticAudioField.BeatsPerMinute,
            [XiphKnownKeys.Conductor] = SemanticAudioField.Conductor,
        };

        private static readonly Dictionary<string, string> s_DistinctLabels = new(StringComparer.Ordinal)
        {
            [XiphKnownKeys.Description] = "Description",
            [XiphKnownKeys.UnsyncedLyrics] = "Unsynced Lyrics",
            [XiphKnownKeys.ContentGroup] = "Content Group",
            [XiphKnownKeys.Date] = "Date",
            [XiphKnownKeys.TrackNumber] = "Track Number",
            [XiphKnownKeys.TrackTotal] = "Track Total",
            [XiphKnownKeys.TotalTracks] = "Total Tracks",
            [XiphKnownKeys.DiscNumber] = "Disc Number",
            [XiphKnownKeys.DiscTotal] = "Disc Total",
            [XiphKnownKeys.TotalDiscs] = "Total Discs",
            [XiphKnownKeys.Tempo] = "Tempo",
        };

        private static readonly Dictionary<string, string> s_KeyToTip = new(StringComparer.Ordinal)
        {
            [XiphKnownKeys.Artist] = SemanticAudioFieldTips.Artist,
            [XiphKnownKeys.AlbumArtist] = SemanticAudioFieldTips.AlbumArtist,
            [XiphKnownKeys.Composer] = SemanticAudioFieldTips.Composer,
            [XiphKnownKeys.Genre] = SemanticAudioFieldTips.Genre,
            [XiphKnownKeys.AmazonId] = SemanticAudioFieldTips.Asin,
            [XiphKnownKeys.BeatsPerMinute] = SemanticAudioFieldTips.Bpm,
        };

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

            // Common keys hit O(1) maps first; MusicBrainz/ASIN catalog rows are a small linear fallback.
            if (s_KeyToSemanticField.TryGetValue(normalized, out var field))
            {
                return SemanticAudioFieldLabels.For(field);
            }

            if (s_DistinctLabels.TryGetValue(normalized, out var label))
            {
                return label;
            }

            var catalogRow = AudioCatalogFieldMaps.All.FirstOrDefault(row =>
                string.Equals(row.XiphKey, normalized, StringComparison.OrdinalIgnoreCase)
            );
            if (catalogRow is not null)
            {
                return SemanticAudioFieldLabels.For(catalogRow.Field);
            }

            return key;
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
            return s_KeyToTip.TryGetValue(normalized, out var tip) ? tip : null;
        }
    }
}
