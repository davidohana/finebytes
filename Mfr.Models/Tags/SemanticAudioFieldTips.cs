using System.Diagnostics.CodeAnalysis;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// User-facing tooltips for <see cref="SemanticAudioField"/> when the label alone is ambiguous.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only ambiguous labels return tips (multi-value / role-confused fields, abbreviations).
    /// Trivial labels (Title, Year, …) return <see langword="null"/> so UI surfaces stay quiet.
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

        /// <summary>Tooltip for <see cref="SemanticAudioField.AmazonId"/> (display label ASIN).</summary>
        public const string Asin = "Amazon Standard Identification Number (product ASIN).";

        /// <summary>Tooltip for <see cref="SemanticAudioField.BeatsPerMinute"/> (display label BPM).</summary>
        public const string Bpm = "Tempo in beats per minute.";

        /// <summary>
        /// Returns a clarifying tooltip for <paramref name="field"/>, or <see langword="null"/> when the label is enough.
        /// </summary>
        /// <param name="field">Semantic field.</param>
        /// <returns>User-visible tooltip text, or <see langword="null"/>.</returns>
        [SuppressMessage(
            "Style",
            "IDE0072:Add missing cases",
            Justification = "Only non-trivial fields return tips; others intentionally return null."
        )]
        public static string? For(SemanticAudioField field)
        {
            return field switch
            {
                SemanticAudioField.Performers => Artist,
                SemanticAudioField.AlbumArtists => AlbumArtist,
                SemanticAudioField.Composers => Composer,
                SemanticAudioField.Genre => Genre,
                SemanticAudioField.AmazonId => Asin,
                SemanticAudioField.BeatsPerMinute => Bpm,
                _ => null,
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
