namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Optional UI hooks for Rename List (wired by the view; set in tests).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="RenameListViewModel.UiHooks"/> is null, or a feature's delegate is null,
    /// that feature is a no-op (export needs <see cref="PickSavePathAsync"/>; Manual Override
    /// needs <see cref="PromptAsync"/>; GO preview-error confirm needs
    /// <see cref="ConfirmPreviewErrorsAsync"/> and aborts when missing).
    /// </para>
    /// </remarks>
    public sealed class RenameListUiHooks
    {
        /// <summary>
        /// Save-path picker; arguments are dialog title, extension without dot, and primary filter label.
        /// </summary>
        /// <remarks>
        /// <para>When null, export is a no-op.</para>
        /// </remarks>
        public Func<string, string, string, Task<string?>>? PickSavePathAsync { get; init; }

        /// <summary>
        /// Error UI when an export write fails; arguments are dialog title and message body.
        /// </summary>
        public Func<string, string, Task>? ShowErrorAsync { get; init; }

        /// <summary>
        /// Text prompt for Manual Override; argument is the styled dialog content.
        /// </summary>
        /// <remarks>
        /// <para>Returns the entered string, or <see langword="null"/> when cancelled.</para>
        /// </remarks>
        public Func<TextInputPrompt, Task<string?>>? PromptAsync { get; init; }

        /// <summary>
        /// GO confirm when preview errors will be ignored; argument is the preview-error row count.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns <see langword="true"/> to continue to commit. When null, GO aborts (same as declining).
        /// </para>
        /// </remarks>
        public Func<int, Task<bool>>? ConfirmPreviewErrorsAsync { get; init; }

        /// <summary>
        /// Post-GO summary when one or more rows failed to commit; argument is the commit-error count.
        /// </summary>
        /// <remarks>
        /// <para>When null, the summary is skipped.</para>
        /// </remarks>
        public Func<int, Task>? ShowCommitErrorSummaryAsync { get; init; }
    }
}
