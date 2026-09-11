namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Optional UI hooks for Rename List (wired by the view; set in tests).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="RenameListViewModel.UiHooks"/> is null, or a feature's delegate is null,
    /// that feature is a no-op (export needs <see cref="PickSavePathAsync"/>; Manual Override
    /// needs <see cref="PromptAsync"/>).
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
        /// Text prompt for Manual Override; arguments are dialog title, prompt line, and default value.
        /// </summary>
        /// <remarks>
        /// <para>Returns the entered string, or <see langword="null"/> when cancelled.</para>
        /// </remarks>
        public Func<string, string, string, Task<string?>>? PromptAsync { get; init; }
    }
}
