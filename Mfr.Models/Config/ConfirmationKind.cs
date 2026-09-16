namespace Mfr.Models.Config
{
    /// <summary>
    /// Suppressible confirmation gates controlled by <see cref="OptionsConfig.SuppressedConfirmations"/> via
    /// <see cref="ConfirmationPolicy"/>.
    /// <para>
    /// Reset Configuration is not a kind — it always confirms and cannot be suppressed.
    /// </para>
    /// </summary>
    public enum ConfirmationKind
    {
        /// <summary>
        /// Confirm before Go when the Rename List has preview errors.
        /// </summary>
        GoWithPreviewErrors = 0,

        /// <summary>
        /// Confirm before replacing a non-empty Filter Chain on preset load.
        /// </summary>
        ReplaceFilterChainOnLoad = 1,

        /// <summary>
        /// Confirm before clearing a non-empty Rename List.
        /// </summary>
        ClearRenameList = 2,

        /// <summary>
        /// Confirm before clearing the Filter Chain.
        /// </summary>
        ClearFilterChain = 3,

        /// <summary>
        /// Confirm before Undo Last / Log-window Undo (prepare preview session; user presses GO to apply).
        /// </summary>
        UndoRename = 4,

        /// <summary>
        /// Confirm before overwriting an existing preset on save.
        /// </summary>
        OverwritePreset = 5,

        /// <summary>
        /// Confirm before deleting one or more presets.
        /// </summary>
        DeletePreset = 6,
    }
}
