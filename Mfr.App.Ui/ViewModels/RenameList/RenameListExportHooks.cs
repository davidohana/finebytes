namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Optional UI hooks for Rename List export (wired by the view; set in tests).
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
        /// Save-path picker; arguments are dialog title, extension without dot, and primary filter label.
        /// </summary>
        /// <remarks>
        /// <para>When null, export is a no-op.</para>
        /// </remarks>
        public Func<string, string, string, Task<string?>>? PickSavePathAsync { get; init; }

        /// <summary>
        /// Error UI when the write fails; arguments are dialog title and message body.
        /// </summary>
        public Func<string, string, Task>? ShowErrorAsync { get; init; }
    }
}
