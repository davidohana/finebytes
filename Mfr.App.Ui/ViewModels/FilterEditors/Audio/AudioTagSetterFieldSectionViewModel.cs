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
        /// Gets full-width rows (lyrics) below the two-column grid.
        /// </summary>
        public IReadOnlyList<AudioTagSetterFieldRowViewModel> FullWidthRows { get; } =
            [.. Rows.Where(row => row.Multiline)];
    }
}
