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
        /// Gets single-line / combo rows laid out in two columns.
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> CompactRows { get; } =
        [.. Rows.Where(row => !row.Multiline)];

        /// <summary>
        /// Gets the left column of compact rows (even indexes).
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> LeftCompactRows { get; } =
        [.. Rows.Where(row => !row.Multiline).Where((_, index) => index % 2 == 0)];

        /// <summary>
        /// Gets the right column of compact rows (odd indexes).
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> RightCompactRows { get; } =
        [.. Rows.Where(row => !row.Multiline).Where((_, index) => index % 2 == 1)];

        /// <summary>
        /// Gets full-width rows (lyrics) below the two-column grid.
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> FullWidthRows { get; } =
        [.. Rows.Where(row => row.Multiline)];
    }
}
