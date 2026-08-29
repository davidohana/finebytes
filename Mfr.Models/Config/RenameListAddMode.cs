namespace Mfr.Models.Config
{
    /// <summary>
    /// Which path kinds become Rename List rows when adding from the File List.
    /// </summary>
    public enum RenameListAddMode
    {
        /// <summary>
        /// Add file rows only (selected files, and files found under selected folders).
        /// </summary>
        Files = 0,

        /// <summary>
        /// Add folder rows only.
        /// </summary>
        Folders = 1,

        /// <summary>
        /// Add both file and folder rows.
        /// </summary>
        FilesAndFolders = 2,
    }

    /// <summary>
    /// Helpers for <see cref="RenameListAddMode"/>.
    /// </summary>
    public static class RenameListAddModeExtensions
    {
        /// <summary>
        /// Returns whether <paramref name="mode"/> includes file rows.
        /// </summary>
        /// <param name="mode">Add mode.</param>
        /// <returns><see langword="true"/> when files are included.</returns>
        public static bool IncludesFiles(this RenameListAddMode mode)
        {
            return mode is RenameListAddMode.Files or RenameListAddMode.FilesAndFolders;
        }

        /// <summary>
        /// Returns whether <paramref name="mode"/> includes folder rows.
        /// </summary>
        /// <param name="mode">Add mode.</param>
        /// <returns><see langword="true"/> when folders are included.</returns>
        public static bool IncludesFolders(this RenameListAddMode mode)
        {
            return mode is RenameListAddMode.Folders or RenameListAddMode.FilesAndFolders;
        }
    }
}
