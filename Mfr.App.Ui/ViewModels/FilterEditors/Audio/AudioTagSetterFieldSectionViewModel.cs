namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// One FieldsetGroup of Audio Tag Setter field rows.
    /// </summary>
    /// <param name="Header">Fieldset header text.</param>
    /// <param name="Rows">Rows shown in this group.</param>
    internal sealed record AudioTagSetterFieldSectionViewModel(
        string Header,
        IReadOnlyList<AudioTagSetterFieldRowViewModel> Rows
    )
    {
        /// <summary>
        /// Gets single-line / combo rows that fit the two-column compact grid (no auto-increment chrome).
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> CompactRows { get; } =
        [.. Rows.Where(row => !row.Multiline && !row.ShowsAutoIncrement)];

        /// <summary>
        /// Gets single-line rows that need the full fieldset width (track + auto-increment).
        /// </summary>
        /// <remarks>
        /// Kept out of the half-width two-column grid so the value editor is not crushed beside
        /// <c>Auto-increment</c>.
        /// </remarks>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> WideCompactRows { get; } =
        [.. Rows.Where(row => !row.Multiline && row.ShowsAutoIncrement)];

        /// <summary>
        /// Gets the left column of compact rows (even indexes).
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> LeftCompactRows { get; } =
        [.. Rows.Where(row => !row.Multiline && !row.ShowsAutoIncrement).Where((_, index) => index % 2 == 0)];

        /// <summary>
        /// Gets the right column of compact rows (odd indexes).
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> RightCompactRows { get; } =
        [.. Rows.Where(row => !row.Multiline && !row.ShowsAutoIncrement).Where((_, index) => index % 2 == 1)];

        /// <summary>
        /// Gets full-width rows (lyrics) below the two-column grid.
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> FullWidthRows { get; } =
        [.. Rows.Where(row => row.Multiline)];
    }
}
