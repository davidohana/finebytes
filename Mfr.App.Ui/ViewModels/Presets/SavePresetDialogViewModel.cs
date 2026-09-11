using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Draft state for the Save Preset dialog (name, description, optional Rename List columns).
    /// </summary>
    public sealed partial class SavePresetDialogViewModel : ViewModelBase
    {
        private readonly IReadOnlyDictionary<string, FilterPreset> _nameToPreset;

        /// <summary>
        /// Initializes draft fields, optionally prefilled from the last-loaded preset.
        /// </summary>
        /// <param name="lastLoaded">
        /// Preset to prefill from when present; when <see langword="null"/>, fields start empty.
        /// </param>
        /// <param name="canUpdate">
        /// When <see langword="true"/> and <paramref name="lastLoaded"/> is set, Update is enabled;
        /// otherwise only Save as new is available.
        /// </param>
        /// <param name="existingPresets">
        /// Current presets for the Name suggestions list; when <see langword="null"/>, suggestions are empty.
        /// </param>
        /// <param name="prefillFromLastLoaded">
        /// When <see langword="true"/> and <paramref name="lastLoaded"/> is set, Name / description /
        /// columns start from that preset. Pass <see langword="false"/> when Applied Filters is empty
        /// so the Name field starts blank.
        /// </param>
        public SavePresetDialogViewModel(
            FilterPreset? lastLoaded = null,
            bool canUpdate = false,
            IEnumerable<FilterPreset>? existingPresets = null,
            bool prefillFromLastLoaded = true
        )
        {
            CanUpdate = canUpdate && lastLoaded is not null;
            OriginalName = lastLoaded?.Name ?? string.Empty;

            var presets = existingPresets?.ToList() ?? [];
            _nameToPreset = presets.ToDictionary(preset => preset.Name, StringComparer.Ordinal);
            ExistingNames =
            [
                .. presets
                    .Select(preset => preset.Name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(name => name, StringComparer.Ordinal),
            ];

            if (lastLoaded is null || !prefillFromLastLoaded)
            {
                return;
            }

            Name = lastLoaded.Name;
            Description = lastLoaded.Description ?? string.Empty;
            SaveRenameListColumns = lastLoaded.VisibleColumns is not null;
        }

        /// <summary>
        /// Gets whether Update is available (last-loaded still present in the store).
        /// </summary>
        public bool CanUpdate { get; }

        /// <summary>
        /// Gets the last-loaded preset name before any edits, or empty when Update is unavailable.
        /// </summary>
        public string OriginalName { get; }

        /// <summary>
        /// Gets sorted existing preset names for the Name suggestions list.
        /// </summary>
        public IReadOnlyList<string> ExistingNames { get; }

        /// <summary>
        /// Gets or sets the preset display name.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanUpdateAction))]
        [NotifyPropertyChangedFor(nameof(CanSaveAsAction))]
        private string _name = string.Empty;

        /// <summary>
        /// Gets or sets the optional preset description.
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// Gets or sets whether the current Rename List visible columns should be stored.
        /// </summary>
        [ObservableProperty]
        private bool _saveRenameListColumns;

        /// <summary>
        /// Gets whether Update is enabled (available and non-blank name).
        /// </summary>
        public bool CanUpdateAction => CanUpdate && !string.IsNullOrWhiteSpace(Name);

        /// <summary>
        /// Gets whether Save as new is enabled (non-blank name).
        /// </summary>
        public bool CanSaveAsAction => !string.IsNullOrWhiteSpace(Name);

        /// <summary>
        /// Gets the trimmed preset name for save.
        /// </summary>
        public string TrimmedName => Name.Trim();

        /// <summary>
        /// Gets the trimmed description, or <see langword="null"/> when blank.
        /// </summary>
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
            var trimmed = value.Trim();
            if (trimmed.Length == 0 || !_nameToPreset.TryGetValue(trimmed, out var preset))
            {
                return;
            }

            Description = preset.Description ?? string.Empty;
            SaveRenameListColumns = preset.VisibleColumns is not null;
        }
    }
}
