namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Optional UI hooks for Manual Override Field (wired by the view; set in tests).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="RenameListViewModel.OverrideHooks"/> is null, or
    /// <see cref="PromptAsync"/> is null, Manual Override is a no-op.
    /// </para>
    /// </remarks>
    public sealed class RenameListOverrideHooks
    {
        /// <summary>
        /// Text prompt; arguments are dialog title, prompt line, and default value.
        /// </summary>
        /// <remarks>
        /// <para>Returns the entered string, or <see langword="null"/> when cancelled.</para>
        /// </remarks>
        public Func<string, string, string, Task<string?>>? PromptAsync { get; init; }
    }
}
