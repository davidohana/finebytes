using Mfr.Models.Tags;

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
            _Row(
                AudioTagSetterFieldKind.Performers,
                AudioTagSetterFieldGroup.Basic,
                "Set artist:",
                SemanticAudioField.Performers,
                watermark: "<parent-folder:1>"
            ),
            _Row(
                AudioTagSetterFieldKind.AlbumArtists,
                AudioTagSetterFieldGroup.Basic,
                "Set album artist:",
                SemanticAudioField.AlbumArtists,
                watermark: "<parent-folder:1>"
            ),
            _Row(
                AudioTagSetterFieldKind.Title,
                AudioTagSetterFieldGroup.Basic,
                "Set title:",
                SemanticAudioField.Title,
                watermark: "<file-name>"
            ),
            _Row(
                AudioTagSetterFieldKind.Album,
                AudioTagSetterFieldGroup.Basic,
                "Set album:",
                SemanticAudioField.Album,
                watermark: "<parent-folder:1>"
            ),
            _Row(
                AudioTagSetterFieldKind.Year,
                AudioTagSetterFieldGroup.Basic,
                "Set year:",
                SemanticAudioField.Year,
                watermark: "2004",
                setterNote: "1–9999 after formatting. Empty or 0 clears."
            ),
            _Row(
                AudioTagSetterFieldKind.Genre,
                AudioTagSetterFieldGroup.Basic,
                "Set genre:",
                SemanticAudioField.Genre,
                watermark: "Rock",
                setterNote: "ID3v1 accepts only predefined genre names.",
                usesGenreCombo: true
            ),
            _Row(
                AudioTagSetterFieldKind.Comment,
                AudioTagSetterFieldGroup.Basic,
                "Set comment:",
                SemanticAudioField.Comment,
                watermark: "Tagged via MFR"
            ),
            _Row(
                AudioTagSetterFieldKind.Track,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set track number:",
                SemanticAudioField.Track,
                watermark: "1",
                setterNote: "Empty always clears. With auto-increment, Rename List index is added to the base before clamping to 255.",
                showsAutoIncrement: true
            ),
            _Row(
                AudioTagSetterFieldKind.TrackCount,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set track count:",
                SemanticAudioField.TrackCount,
                watermark: "12",
                setterNote: "Empty or 0 clears; otherwise 1–255."
            ),
            _Row(
                AudioTagSetterFieldKind.Disc,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set disc:",
                SemanticAudioField.Disc,
                watermark: "1",
                setterNote: "Empty or 0 clears; otherwise 1–255."
            ),
            _Row(
                AudioTagSetterFieldKind.DiscCount,
                AudioTagSetterFieldGroup.TrackDisc,
                "Set disc count:",
                SemanticAudioField.DiscCount,
                watermark: "2",
                setterNote: "Empty or 0 clears; otherwise 1–255."
            ),
            _Row(
                AudioTagSetterFieldKind.Composers,
                AudioTagSetterFieldGroup.Extended,
                "Set composer:",
                SemanticAudioField.Composers,
                watermark: "J. S. Bach"
            ),
            _Row(
                AudioTagSetterFieldKind.Conductor,
                AudioTagSetterFieldGroup.Extended,
                "Set conductor:",
                SemanticAudioField.Conductor,
                watermark: "Karajan"
            ),
            _Row(
                AudioTagSetterFieldKind.Grouping,
                AudioTagSetterFieldGroup.Extended,
                "Set grouping:",
                SemanticAudioField.Grouping,
                watermark: "Suite"
            ),
            _Row(
                AudioTagSetterFieldKind.Copyright,
                AudioTagSetterFieldGroup.Extended,
                "Set copyright:",
                SemanticAudioField.Copyright,
                watermark: "© 2004"
            ),
            _Row(
                AudioTagSetterFieldKind.BeatsPerMinute,
                AudioTagSetterFieldGroup.Extended,
                "Set BPM:",
                SemanticAudioField.BeatsPerMinute,
                watermark: "120",
                setterNote: "1–65535 after formatting. Empty or 0 clears."
            ),
            _Row(
                AudioTagSetterFieldKind.Lyrics,
                AudioTagSetterFieldGroup.Extended,
                "Set lyrics:",
                SemanticAudioField.Lyrics,
                watermark: "Verse one",
                multiline: true
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

        /// <summary>
        /// Builds a catalog row whose tip is the shared semantic meaning plus an optional setter note.
        /// </summary>
        private static AudioTagSetterFieldChoice _Row(
            AudioTagSetterFieldKind kind,
            AudioTagSetterFieldGroup group,
            string label,
            SemanticAudioField field,
            string watermark = "",
            string? setterNote = null,
            bool showsAutoIncrement = false,
            bool multiline = false,
            bool usesGenreCombo = false
        )
        {
            var meaning = SemanticAudioFieldTips.For(field);
            var tip = setterNote is null ? meaning : $"{meaning} {setterNote}";
            return new(kind, group, label, tip, watermark, showsAutoIncrement, multiline, usesGenreCombo);
        }
    }
}
