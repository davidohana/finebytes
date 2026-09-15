namespace Mfr.App.Ui.ViewModels.FilterChain
{
    /// <summary>
    /// Optional UI hooks for Filter Chain (wired by the view; set in tests).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="FilterChainViewModel.UiHooks"/> is null, or <see cref="ConfirmClearAsync"/> is null
    /// while confirmation policy requires a Clear confirm, Clear aborts.
    /// </para>
    /// </remarks>
    public sealed class FilterChainUiHooks
    {
        /// <summary>
        /// Confirm before clearing the Filter Chain when confirmation policy requires it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns <see langword="true"/> to clear. When null while confirm is required, Clear aborts.
        /// </para>
        /// </remarks>
        public Func<Task<bool>>? ConfirmClearAsync { get; init; }
    }
}
