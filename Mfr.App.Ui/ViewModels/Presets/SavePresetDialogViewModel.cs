using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Draft state for the Save Preset As dialog (name, description, optional Rename List columns).
    /// </summary>
    public sealed partial class SavePresetDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes draft fields, optionally prefilled from the last-loaded preset.
        /// </summary>
        /// <param name="lastLoaded">
        /// Preset to prefill from when present; when <see langword="null"/>, fields start empty.
        /// </param>
        public SavePresetDialogViewModel(FilterPreset? lastLoaded = null)
        {
            if (lastLoaded is null)
            {
                return;
            }

            Name = lastLoaded.Name;
            Description = lastLoaded.Description ?? string.Empty;
            SaveRenameListColumns = lastLoaded.VisibleColumns is not null;
        }

        /// <summary>
        /// Gets or sets the preset display name.
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// Gets or sets the optional preset description.
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// Gets or sets whether the current Rename List visible columns should be stored on the preset.
        /// </summary>
        [ObservableProperty]
        private bool _saveRenameListColumns;

        /// <summary>
        /// Gets whether OK is allowed (non-whitespace name).
        /// </summary>
        public bool CanConfirm => !string.IsNullOrWhiteSpace(Name);

        /// <summary>
        /// Gets the trimmed preset name for save.
        /// </summary>
        /// <returns>Trimmed name, or empty when blank.</returns>
        public string TrimmedName => Name.Trim();

        /// <summary>
        /// Gets the trimmed description, or <see langword="null"/> when blank.
        /// </summary>
        /// <returns>Trimmed description text, or <see langword="null"/>.</returns>
        public string? TrimmedDescriptionOrNull
        {
            get
            {
                var trimmed = Description.Trim();
                return trimmed.Length == 0 ? null : trimmed;
            }
        }

        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(CanConfirm));
        }
    }
}
