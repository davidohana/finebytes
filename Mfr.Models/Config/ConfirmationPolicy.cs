namespace Mfr.Models.Config
{
    /// <summary>
    /// Reads <see cref="UiConfig.ConfirmationPrompts"/> from <see cref="ConfigStore.Config"/> to decide whether a gated
    /// confirm should show.
    /// </summary>
    public static class ConfirmationPolicy
    {
        /// <summary>
        /// Returns whether the UI should confirm for <paramref name="kind"/> at the current prompts level.
        /// </summary>
        /// <param name="kind">Gated confirmation kind (not always-confirm actions).</param>
        /// <returns><see langword="true"/> when a confirm dialog should be shown.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined member.</exception>
        public static bool ShouldConfirm(ConfirmationKind kind)
        {
            var level = ConfigStore.Config.Ui.ConfirmationPrompts;
            return kind switch
            {
                ConfirmationKind.GoWithPreviewErrors => level is ConfirmationPrompts.Normal or ConfirmationPrompts.More,
                ConfirmationKind.ReplaceAppliedFiltersOnLoad
                or ConfirmationKind.ClearRenameList
                or ConfirmationKind.ClearAppliedFilters => level is ConfirmationPrompts.More,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null),
            };
        }
    }
}
