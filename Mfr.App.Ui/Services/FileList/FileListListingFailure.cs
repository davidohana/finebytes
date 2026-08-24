namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Why listing the current folder failed.
    /// </summary>
    internal enum FileListListingFailure
    {
        /// <summary>
        /// Listing succeeded.
        /// </summary>
        None,

        /// <summary>
        /// The folder exists but cannot be read.
        /// </summary>
        AccessDenied,

        /// <summary>
        /// The folder path does not exist.
        /// </summary>
        NotFound,

        /// <summary>
        /// A network probe exceeded the File List timeout.
        /// </summary>
        TimedOut,

        /// <summary>
        /// The folder could not be listed for another I/O reason.
        /// </summary>
        Unavailable,
    }
}
