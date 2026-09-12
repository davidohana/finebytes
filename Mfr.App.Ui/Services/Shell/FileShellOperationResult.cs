namespace Mfr.App.Ui.Services.Shell
{
    /// <summary>
    /// Outcome of a shell file delete/copy/move request.
    /// </summary>
    public enum FileShellOperationResult
    {
        /// <summary>
        /// The operation completed without cancellation.
        /// </summary>
        Succeeded = 0,

        /// <summary>
        /// The user cancelled the shell UI (or all items were aborted).
        /// </summary>
        Cancelled = 1,

        /// <summary>
        /// The operation failed or could not be started.
        /// </summary>
        Failed = 2,
    }
}
