namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One row in the Rename List grid.
    /// </summary>
    public sealed class RenameListEntry
    {
        /// <summary>
        /// Gets the file-or-folder label shown in the File/Folder column.
        /// </summary>
        public string FileFolder { get; init; } = string.Empty;

        /// <summary>
        /// Gets the parent folder path shown in the Parent Folder column.
        /// </summary>
        public string ParentFolder { get; init; } = string.Empty;

        /// <summary>
        /// Gets the original full file name shown in the Full File Name column.
        /// </summary>
        public string FullFileName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the preview full file name shown in the Full File Name (Preview) column.
        /// </summary>
        public string FullFileNamePreview { get; init; } = string.Empty;
    }
}
