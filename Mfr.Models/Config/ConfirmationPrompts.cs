namespace Mfr.Models.Config
{
    /// <summary>
    /// How often the UI asks for confirmation before potentially disruptive actions.
    /// <para>Persisted as <c>ui.confirmationPrompts</c> (camelCase enum member name, e.g. <c>normal</c>).</para>
    /// </summary>
    public enum ConfirmationPrompts
    {
        /// <summary>
        /// Skip optional confirms (Go with preview errors proceeds; replace-on-load and clears do not confirm).
        /// </summary>
        Fewer = 0,

        /// <summary>
        /// Default: confirm Go with preview errors; skip replace-on-load and clear confirms.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Confirm Go with preview errors, replace Applied Filters on preset load, and Clear Rename List / Applied Filters.
        /// </summary>
        More = 2,
    }
}
