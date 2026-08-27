namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Sort header state for the three visible, sortable Rename List columns.
    /// </summary>
    /// <param name="FileFolder">File/Folder column state.</param>
    /// <param name="ParentFolder">Parent Folder column state.</param>
    /// <param name="FullFileName">Full File Name column state.</param>
    public sealed record RenameListColumnSortStates(
        RenameListColumnSortState FileFolder,
        RenameListColumnSortState ParentFolder,
        RenameListColumnSortState FullFileName
    )
    {
        /// <summary>
        /// All visible columns inactive (Auto-Sort off).
        /// </summary>
        public static RenameListColumnSortStates Inactive { get; } = new(default, default, default);
    }
}
