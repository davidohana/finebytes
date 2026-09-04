namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Background Rename List operation shown in the progress dialog.
    /// </summary>
    public enum RenameListProgressOperation
    {
        /// <summary>
        /// Resolving and appending rename sources.
        /// </summary>
        Add,

        /// <summary>
        /// Reading metadata for visible columns or Auto-Sort keys.
        /// </summary>
        MetadataHydrate,

        /// <summary>
        /// Re-reading original fields from disk for every row.
        /// </summary>
        Refresh,

        /// <summary>
        /// Applying the filter chain to produce Rename List preview values.
        /// </summary>
        Preview,
    }
}
