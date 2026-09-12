namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Optional UI hooks for Applied Filters (wired by the view; set in tests).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see cref="AppliedFiltersViewModel.UiHooks"/> is null, or <see cref="ConfirmClearAsync"/> is null
    /// while confirmation policy requires a Clear confirm, Clear aborts.
    /// </para>
    /// </remarks>
    public sealed class AppliedFiltersUiHooks
    {
        /// <summary>
        /// Confirm before removing all Applied Filters when confirmation policy requires it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns <see langword="true"/> to clear. When null while confirm is required, Clear aborts.
        /// </para>
        /// </remarks>
        public Func<Task<bool>>? ConfirmClearAsync { get; init; }
    }
}
