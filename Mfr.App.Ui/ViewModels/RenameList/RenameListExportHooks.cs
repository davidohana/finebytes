namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Optional UI hooks for Export Name List (wired by the view; set in tests).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="RenameListViewModel.ExportHooks"/> is null, or
    /// <see cref="PickSavePathAsync"/> is null, export is a no-op.
    /// </para>
    /// </remarks>
    public sealed class RenameListExportHooks
    {
        /// <summary>
        /// Save-path picker; when null, export is a no-op.
        /// </summary>
        public Func<Task<string?>>? PickSavePathAsync { get; init; }

        /// <summary>
        /// Post-save prompt for opening the exported file; argument is the saved path.
        /// </summary>
        /// <remarks>
        /// <para>Return <see langword="true"/> to open with the default app.</para>
        /// </remarks>
        public Func<string, Task<bool>>? ConfirmEditAsync { get; init; }

        /// <summary>
        /// Error UI when the write fails; arguments are dialog title and message body.
        /// </summary>
        public Func<string, string, Task>? ShowErrorAsync { get; init; }
    }
}
