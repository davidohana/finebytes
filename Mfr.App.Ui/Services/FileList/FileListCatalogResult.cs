namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Rows from <see cref="FileListCatalog.List"/> plus a failure when a folder could not be read.
    /// </summary>
    /// <param name="Items">Listed rows; empty when listing failed.</param>
    /// <param name="Failure">Why listing failed, or <see cref="FileListListingFailure.None"/>.</param>
    internal readonly record struct FileListCatalogResult(
        IReadOnlyList<FileListListedItem> Items,
        FileListListingFailure Failure
    )
    {
        /// <summary>
        /// Successful listing of <paramref name="items"/>.
        /// </summary>
        /// <param name="items">Rows to show.</param>
        /// <returns>A result with <see cref="FileListListingFailure.None"/>.</returns>
        public static FileListCatalogResult Ok(IReadOnlyList<FileListListedItem> items)
        {
            return new FileListCatalogResult(items, FileListListingFailure.None);
        }

        /// <summary>
        /// Failed listing with no rows.
        /// </summary>
        /// <param name="failure">Why listing failed.</param>
        /// <returns>An empty result with <paramref name="failure"/>.</returns>
        public static FileListCatalogResult Failed(FileListListingFailure failure)
        {
            return new FileListCatalogResult([], failure);
        }
    }
}
