using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Outcome of renaming a named preset.
    /// </summary>
    public enum PresetRenameStatus
    {
        /// <summary>
        /// Preset was renamed and persisted.
        /// </summary>
        Success,

        /// <summary>
        /// Requested name matched the current name after trim (no dictionary change).
        /// </summary>
        Unchanged,

        /// <summary>
        /// New name was blank or whitespace-only.
        /// </summary>
        BlankName,

        /// <summary>
        /// Another preset already uses the requested name.
        /// </summary>
        NameTaken,

        /// <summary>
        /// Current name was not found in the manager.
        /// </summary>
        NotFound,
    }

    /// <summary>
    /// Result of <see cref="AppliedFilters.AppliedFiltersViewModel.RenamePreset"/>.
    /// </summary>
    /// <param name="Status">Rename outcome.</param>
    /// <param name="Preset">
    /// The unchanged or renamed preset when <see cref="Status"/> is
    /// <see cref="PresetRenameStatus.Unchanged"/> or <see cref="PresetRenameStatus.Success"/>;
    /// otherwise <see langword="null"/>.
    /// </param>
    public readonly record struct PresetRenameResult(PresetRenameStatus Status, FilterPreset? Preset);
}
