namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Catalog metadata for one Audio Tag Setter field row.
    /// </summary>
    /// <param name="Kind">Which overlay field this row edits.</param>
    /// <param name="Group">Which options fieldset this row belongs to.</param>
    /// <param name="Label">Three-state checkbox content (MFR7 “Set …:” wording where it existed).</param>
    /// <param name="Tip">Short per-field tooltip body.</param>
    /// <param name="Watermark">Optional text-box watermark (format examples / defaults).</param>
    /// <param name="ShowsAutoIncrement">When true, show the track auto-increment checkbox beside the value.</param>
    /// <param name="Multiline">When true, use a taller multi-line value box (lyrics).</param>
    /// <param name="UsesGenreCombo">When true, use an editable genre ComboBox with ID3v1 suggestions.</param>
    internal sealed record AudioTagSetterFieldChoice(
        AudioTagSetterFieldKind Kind,
        AudioTagSetterFieldGroup Group,
        string Label,
        string Tip,
        string Watermark = "",
        bool ShowsAutoIncrement = false,
        bool Multiline = false,
        bool UsesGenreCombo = false
    )
    {
        /// <summary>
        /// All setter fields in editor order (fieldset groups: Basic, Track / Disc, Extended).
        /// </summary>
        public static IReadOnlyList<AudioTagSetterFieldChoice> All { get; } =
        [
            new(
                AudioTagSetterFieldKind.Performers,
                AudioTagSetterFieldGroup.Basic,
                "Set performer(s):",
                "Primary artists. Use ';' to separate multiple values.",
                Watermark: "<parent-folder:1>"
            ),
            new(
                AudioTagSetterFieldKind.AlbumArtists,
                AudioTagSetterFieldGroup.Basic,
                "Set album artist(s):",
                "Album artists. Use ';' to separate multiple values.",
                Watermark: "<parent-folder:1>"
            ),
            new(
                AudioTagSetterFieldKind.Title,
                AudioTagSetterFieldGroup.Basic,
                "Set title:",
                "Track title.",
                Watermark: "<file-name>"
            ),
            new(
                AudioTagSetterFieldKind.Album,
                AudioTagSetterFieldGroup.Basic,
                "Set album:",
                "Album name.",
                Watermark: "<parent-folder:1>"
            ),
            new(
                AudioTagSetterFieldKind.Year,
                AudioTagSetterFieldGroup.Basic,
                "Set year:",
                "Release year 1–9999 after formatting. Empty or 0 clears.",
                Watermark: "2004"
            ),
            new(
                AudioTagSetterFieldKind.Genre,
                AudioTagSetterFieldGroup.Basic,
                "Set genre(s):",
                "Use ';' to separate multiple values. ID3v1 accepts only predefined genre names.",
                Watermark: "Rock",
                UsesGenreCombo: true
            ),
            new(
                AudioTagSetterFieldKind.Comment,
                AudioTagSetterFieldGroup.Basic,
                "Set comment:",
                "Comment text.",
                Watermark: "Tagged via MFR"
            ),
            new(
                AudioTagSetterFieldKind.Track,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set track number:",
                "Track index after formatting (0–255 base). Empty always clears. With auto-increment, Rename List index is added to the base before clamping to 255.",
                Watermark: "1",
                ShowsAutoIncrement: true
            ),
            new(
                AudioTagSetterFieldKind.TrackCount,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set track count:",
                "Total tracks (n of m). Empty or 0 clears; otherwise 1–255.",
                Watermark: "12"
            ),
            new(
                AudioTagSetterFieldKind.Disc,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set disc:",
                "Disc index. Empty or 0 clears; otherwise 1–255.",
                Watermark: "1"
            ),
            new(
                AudioTagSetterFieldKind.DiscCount,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set disc count:",
                "Total discs. Empty or 0 clears; otherwise 1–255.",
                Watermark: "2"
            ),
            new(
                AudioTagSetterFieldKind.Composers,
                AudioTagSetterFieldGroup.Extended,
                "Set composer(s):",
                "Composers. Use ';' to separate multiple values.",
                Watermark: "J. S. Bach"
            ),
            new(
                AudioTagSetterFieldKind.Conductor,
                AudioTagSetterFieldGroup.Extended,
                "Set conductor:",
                "Conductor or director.",
                Watermark: "Karajan"
            ),
            new(
                AudioTagSetterFieldKind.Grouping,
                AudioTagSetterFieldGroup.Extended,
                "Set grouping:",
                "Content group / work title.",
                Watermark: "Suite"
            ),
            new(
                AudioTagSetterFieldKind.Copyright,
                AudioTagSetterFieldGroup.Extended,
                "Set copyright:",
                "Copyright notice.",
                Watermark: "© 2004"
            ),
            new(
                AudioTagSetterFieldKind.BeatsPerMinute,
                AudioTagSetterFieldGroup.Extended,
                "Set BPM:",
                "Tempo 1–65535 after formatting. Empty or 0 clears.",
                Watermark: "120"
            ),
            new(
                AudioTagSetterFieldKind.Lyrics,
                AudioTagSetterFieldGroup.Extended,
                "Set lyrics:",
                "Unsynchronised lyrics.",
                Watermark: "Verse one",
                Multiline: true
            ),
        ];

        /// <summary>
        /// Fieldset header for a catalog group.
        /// </summary>
        /// <param name="group">Catalog group.</param>
        /// <returns>Header text for <see cref="AudioTagSetterFieldSectionViewModel"/>.</returns>
        public static string HeaderFor(AudioTagSetterFieldGroup group)
        {
            return group switch
            {
                AudioTagSetterFieldGroup.Basic => "Basic",
                AudioTagSetterFieldGroup.TrackDisc => "Track / Disc",
                AudioTagSetterFieldGroup.Extended => "Extended",
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
            };
        }
    }
}
