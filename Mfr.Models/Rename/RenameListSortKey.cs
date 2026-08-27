namespace Mfr.Models.Rename
{
    /// <summary>
    /// Rename List columns that can participate in Auto-Sort (original fields only).
    /// </summary>
    public enum RenameListSortColumn
    {
        /// <summary>
        /// File vs Folder (<c>File</c> / <c>Folder</c>).
        /// </summary>
        FileFolder = 0,

        /// <summary>
        /// Parent directory path.
        /// </summary>
        ParentFolder = 1,

        /// <summary>
        /// File name including extension.
        /// </summary>
        FullFileName = 2,

        /// <summary>
        /// Absolute full path (default secondary key; not a visible grid column yet).
        /// </summary>
        FullPath = 3,
    }

    /// <summary>
    /// One Auto-Sort key: column plus ascending/descending.
    /// </summary>
    /// <param name="Column">Column to compare.</param>
    /// <param name="Descending">When <see langword="true"/>, reverse that column's order.</param>
    public readonly record struct RenameListSortKey(RenameListSortColumn Column, bool Descending = false)
    {
        /// <summary>
        /// Default Auto-Sort keys: File/Folder then Full Path (MFR7). Empty session value disables Auto-Sort.
        /// </summary>
        public static IReadOnlyList<RenameListSortKey> DefaultKeys { get; } =
        [new RenameListSortKey(RenameListSortColumn.FileFolder), new RenameListSortKey(RenameListSortColumn.FullPath)];
    }
}
