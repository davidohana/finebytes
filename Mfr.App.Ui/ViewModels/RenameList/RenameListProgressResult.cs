namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Outcome of a cancelable Rename List progress run.
    /// </summary>
    public enum RenameListProgressResult
    {
        /// <summary>
        /// Work finished without user cancel.
        /// </summary>
        Completed,

        /// <summary>
        /// User canceled (or a second run was refused while busy); discard/rollback as needed.
        /// </summary>
        Canceled,

        /// <summary>
        /// User canceled an Add with Keep added; staging batch was kept (no discard rollback).
        /// </summary>
        CanceledKeep,
    }
}
