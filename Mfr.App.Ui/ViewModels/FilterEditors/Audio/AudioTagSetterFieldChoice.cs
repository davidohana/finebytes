namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Catalog metadata for one Audio Tag Setter field row.
    /// </summary>
    /// <param name="Kind">Which overlay field this row edits.</param>
    /// <param name="Label">Three-state checkbox content (MFR7 “Set …:” wording where it existed).</param>
    /// <param name="Tip">Short per-field tooltip body.</param>
    /// <param name="Watermark">Optional text-box watermark (format examples / defaults).</param>
    /// <param name="ShowsAutoIncrement">When true, show the track auto-increment checkbox beside the value.</param>
    /// <param name="Multiline">When true, use a taller multi-line value box (lyrics).</param>
    internal sealed record AudioTagSetterFieldChoice(
        AudioTagSetterFieldKind Kind,
        string Label,
        string Tip,
        string Watermark = "",
        bool ShowsAutoIncrement = false,
        bool Multiline = false
    )
    {
        /// <summary>
        /// All setter fields in editor order (MFR7 core fields first, then extended overlay fields).
        /// </summary>
        public static IReadOnlyList<AudioTagSetterFieldChoice> All { get; } =
        [
            new(
                AudioTagSetterFieldKind.Track,
                "Set track number:",
                "Track index after formatting (0–255 base). Empty always clears. With auto-increment, Rename List index is added to the base before clamping to 255.",
                Watermark: "1",
                ShowsAutoIncrement: true
            ),
            new(
                AudioTagSetterFieldKind.TrackCount,
                "Set track count:",
                "Total tracks (n of m). Empty or 0 clears; otherwise 1–255.",
                Watermark: "12"
            ),
            new(
                AudioTagSetterFieldKind.Disc,
                "Set disc:",
                "Disc index. Empty or 0 clears; otherwise 1–255.",
                Watermark: "1"
            ),
            new(
                AudioTagSetterFieldKind.DiscCount,
                "Set disc count:",
                "Total discs. Empty or 0 clears; otherwise 1–255.",
                Watermark: "2"
            ),
            new(
                AudioTagSetterFieldKind.Performers,
                "Set performer(s):",
                "Primary artists. Use ';' for multiple values. May include formatter tokens.",
                Watermark: "<parent-folder:1>"
            ),
            new(
                AudioTagSetterFieldKind.AlbumArtists,
                "Set album artist(s):",
                "Album artists. Use ';' for multiple values. May include formatter tokens.",
                Watermark: "<parent-folder:1>"
            ),
            new(
                AudioTagSetterFieldKind.Title,
                "Set title:",
                "Track title. May include formatter tokens.",
                Watermark: "<file-name>"
            ),
            new(
                AudioTagSetterFieldKind.Album,
                "Set album:",
                "Album name. May include formatter tokens.",
                Watermark: "<parent-folder:1>"
            ),
            new(
                AudioTagSetterFieldKind.Year,
                "Set year:",
                "Release year 1–9999 after formatting. Empty or 0 clears.",
                Watermark: "2004"
            ),
            new(
                AudioTagSetterFieldKind.Genre,
                "Set genre(s):",
                "Genre text. Use ';' for multiple values. ID3v1 accepts only predefined genre names.",
                Watermark: "Rock"
            ),
            new(
                AudioTagSetterFieldKind.Comment,
                "Set comment:",
                "Comment text. May include formatter tokens.",
                Watermark: "Tagged via MFR"
            ),
            new(
                AudioTagSetterFieldKind.Composers,
                "Set composer(s):",
                "Composers. Use ';' for multiple values. May include formatter tokens.",
                Watermark: "J. S. Bach"
            ),
            new(
                AudioTagSetterFieldKind.Conductor,
                "Set conductor:",
                "Conductor or director. May include formatter tokens.",
                Watermark: "Karajan"
            ),
            new(
                AudioTagSetterFieldKind.Grouping,
                "Set grouping:",
                "Content group / work title. May include formatter tokens.",
                Watermark: "Suite"
            ),
            new(
                AudioTagSetterFieldKind.Copyright,
                "Set copyright:",
                "Copyright notice. May include formatter tokens.",
                Watermark: "© 2004"
            ),
            new(
                AudioTagSetterFieldKind.BeatsPerMinute,
                "Set BPM:",
                "Tempo 1–65535 after formatting. Empty or 0 clears.",
                Watermark: "120"
            ),
            new(
                AudioTagSetterFieldKind.Lyrics,
                "Set lyrics:",
                "Unsynchronised lyrics. May include formatter tokens.",
                Watermark: "Verse one",
                Multiline: true
            ),
        ];
    }
}
