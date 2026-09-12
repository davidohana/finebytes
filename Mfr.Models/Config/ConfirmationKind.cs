namespace Mfr.Models.Config
{
    /// <summary>
    /// Confirmation gates controlled by <see cref="ConfirmationPrompts"/> via <see cref="ConfirmationPolicy"/>.
    /// <para>
    /// Irreversible always-confirm actions (reset / delete / overwrite preset) are not represented here.
    /// </para>
    /// </summary>
    public enum ConfirmationKind
    {
        /// <summary>
        /// Confirm before Go when the Rename List has preview errors.
        /// </summary>
        GoWithPreviewErrors = 0,

        /// <summary>
        /// Confirm before replacing a non-empty Applied Filters chain on preset load.
        /// </summary>
        ReplaceAppliedFiltersOnLoad = 1,

        /// <summary>
        /// Confirm before clearing a non-empty Rename List.
        /// </summary>
        ClearRenameList = 2,

        /// <summary>
        /// Confirm before removing all Applied Filters.
        /// </summary>
        ClearAppliedFilters = 3,
    }
}
